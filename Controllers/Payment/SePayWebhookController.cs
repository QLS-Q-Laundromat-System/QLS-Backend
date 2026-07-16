using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.DTOs.Integrations;
using QLS.Backend.Interfaces;
using QLS.Backend.Models;
using QLS.Backend.Models.Enums;
using QLS.Backend.Services;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using QLS.Backend.Services.Machine;
using QLS.Backend.Interfaces.Loyalty;

namespace QLS.Backend.Controllers
{
    [ApiController]
    [Route("api/webhooks/sepay")]
    public class SePayWebhookController : ControllerBase
    {
        public class SePayTestRequest { public string PaymentCode { get; set; } = ""; }
        private readonly AppDbContext _context;
        private readonly IMachineService _machineService;
        private readonly IZigbeeService _zigbeeService;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _env;
        private readonly IHardwareTrackerService _hardwareTracker;
        private readonly ILoyaltyService _loyaltyService;
        private readonly ILogger<SePayWebhookController> _logger;

        public SePayWebhookController(
            AppDbContext context,
            IMachineService machineService,
            IZigbeeService zigbeeService,
            IConfiguration configuration,
            IHostEnvironment env,
            IHardwareTrackerService hardwareTracker,
            ILoyaltyService loyaltyService,
            ILogger<SePayWebhookController> logger)
        {
            _context = context;
            _machineService = machineService;
            _zigbeeService = zigbeeService;
            _configuration = configuration;
            _env = env;
            _hardwareTracker = hardwareTracker;
            _loyaltyService = loyaltyService;
            _logger = logger;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Receive()
        {
            // Đọc raw body để verify signature và dùng cho deserialization
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, true, 1024, true);
            var rawBody = await reader.ReadToEndAsync();
            Request.Body.Position = 0; // Reset để nếu có middleware khác cần thì vẫn còn

            SePayWebhookDto? dto;
            try 
            {
                dto = JsonSerializer.Deserialize<SePayWebhookDto>(rawBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SePay Webhook] Failed to deserialize payload.");
                return BadRequest(new { message = "Invalid JSON payload" });
            }

            if (dto == null) return BadRequest();

            _logger.LogInformation("[SePay Webhook] Received transaction {Id} for amount {Amount}", dto.Id, dto.TransferAmount);

            var paymentConfig = await _context.PaymentConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.AccountNumber == dto.AccountNumber &&
                    p.IsActive &&
                    p.Provider == "SEPAY");

            var webhookSecret = paymentConfig?.SecretKey;
            var signatureHeader = Request.Headers["X-SePay-Signature"].ToString();
            var timestampHeader = Request.Headers["X-SePay-Timestamp"].ToString();

            if (string.IsNullOrWhiteSpace(webhookSecret) ||
                string.IsNullOrWhiteSpace(signatureHeader) ||
                !TryParseFreshTimestamp(timestampHeader, out _))
            {
                _logger.LogWarning("[SePay Webhook] Rejected transaction {Id}: missing or invalid authentication metadata.", dto.Id);
                return Unauthorized(new { message = "Invalid webhook authentication." });
            }

            var dataToSign = $"{timestampHeader}.{rawBody}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
            var expectedSignature = $"sha256={Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign))).ToLowerInvariant()}";
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(signatureHeader.Trim()),
                    Encoding.UTF8.GetBytes(expectedSignature)))
            {
                _logger.LogWarning("[SePay Webhook] Rejected transaction {Id}: invalid signature.", dto.Id);
                return Unauthorized(new { message = "Invalid webhook authentication." });
            }

            var paymentCode = ExtractPaymentCode(dto.Code) ?? ExtractPaymentCode(dto.Content);
            var paymentCodeMatch = paymentCode == null
                ? null
                : await _context.MachineSessions.FirstOrDefaultAsync(s =>
                    s.PaymentCode == paymentCode &&
                    (s.Status == MachineSessionStatus.PendingPayment ||
                     s.Status == MachineSessionStatus.PaidWaitingForStart ||
                     s.Status == MachineSessionStatus.Running));

            if (paymentCodeMatch == null)
            {
                _logger.LogWarning("[SePay Webhook] No matching session found for transaction {Id}.", dto.Id);
                return Ok(new { success = true }); 
            }

            return await ProcessPaymentAsync(dto, paymentCodeMatch);
        }

        /// <summary>
        /// API Sandbox để test local: POST /api/webhooks/sepay/test-pay
        /// </summary>
        [HttpPost("test-pay")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> TestPayment([FromBody] SePayTestRequest request)
        {
            if (!_env.IsDevelopment() || !_configuration.GetValue<bool>("SePay:EnableTestEndpoints"))
            {
                _logger.LogWarning("[Sandbox] Từ chối TestPayment vì test endpoint đang bị tắt.");
                return NotFound();
            }

            _logger.LogInformation("[Sandbox] Gia lap thanh toan cho ma: {Code}", request.PaymentCode);

            var session = await _context.MachineSessions
                .FirstOrDefaultAsync(s => s.PaymentCode == request.PaymentCode && 
                                        (s.Status == MachineSessionStatus.PendingPayment || s.Status == MachineSessionStatus.PaidWaitingForStart || s.Status == MachineSessionStatus.Running));

            if (session == null) return NotFound(new { message = "Khong tim thay session dang cho" });

            var mockDto = new SePayWebhookDto
            {
                Id = new Random().Next(100000, 999999),
                Content = session.PaymentCode,
                TransferAmount = session.PricePaid,
                TransferType = "in"
            };

            return await ProcessPaymentAsync(mockDto, session);
        }

        private async Task<IActionResult> ProcessPaymentAsync(SePayWebhookDto dto, MachineSession paymentCodeMatch)
        {
            // 0. Kiểm tra chống trùng lặp (Idempotency)
            // Nếu webhook được gọi lại với cùng 1 ID giao dịch, ta bỏ qua và trả về thành công
            var existingTransaction = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t => t.GatewayTransactionId == dto.Id.ToString());

            if (existingTransaction != null)
            {
                if (existingTransaction.Status == "Success")
                {
                    _logger.LogInformation("[SePay Webhook] Transaction {Id} already completed.", dto.Id);
                    return Ok(new { success = true });
                }

                _logger.LogError("[SePay Webhook] Transaction {Id} is in state {Status} and requires recovery.", dto.Id, existingTransaction.Status);
                return StatusCode(500, new { message = "Payment transaction requires recovery." });
            }

            // 1. Tạo bản ghi giao dịch (Audit Log)
            var transaction = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                Amount = dto.TransferAmount,
                PaymentMethod = "SePay",
                GatewayTransactionId = dto.Id.ToString(),
                TransactionContent = dto.Content,
                RawData = JsonSerializer.Serialize(dto),
                Status = "Processing"
            };

            _context.PaymentTransactions.Add(transaction);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "[SePay Webhook] Duplicate transaction race for {Id}.", dto.Id);
                return StatusCode(500, new { message = "Payment transaction is being processed." });
            }

            try
            {
                // Chỉ xử lý giao dịch tiền vào
                if (dto.TransferType != "in")
                {
                    _logger.LogInformation("[SePay Webhook] Ignoring transaction {Id} because type is {Type}", dto.Id, dto.TransferType);
                    transaction.Status = "Ignored (Not Inbound)";
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true });
                }

                _logger.LogInformation("[Process] Found session {SessionId} for machine {MachineId}", paymentCodeMatch.Id, paymentCodeMatch.MachineId);

                // 3. Kiểm tra số tiền (Cho phép sai số nhỏ nếu cần, hoặc khớp chính xác)
                if (dto.TransferAmount < paymentCodeMatch.PricePaid)
                {
                    _logger.LogWarning("[SePay Webhook] Amount mismatch for session {SessionId}. Expected: {Expected}, Received: {Received}", 
                        paymentCodeMatch.Id, paymentCodeMatch.PricePaid, dto.TransferAmount);
                    transaction.Status = "MismatchAmount";
                    transaction.MachineSessionId = paymentCodeMatch.Id;
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true });
                }

                // 4. Xác nhận thanh toán (nếu chưa xác nhận)
                transaction.MachineSessionId = paymentCodeMatch.Id;
                transaction.Status = "Success";
                
                // Cập nhật trạng thái session sang Running
                await _machineService.ConfirmPaymentAsync(paymentCodeMatch.Id, dto.Id.ToString());
                await _loyaltyService.EnsureClaimTokenForPaymentAsync(paymentCodeMatch, transaction);

                // 5. Kích hoạt ESP32 qua Zigbee
                var machine = await _context.Machines
                    .Include(m => m.Store)
                    .FirstOrDefaultAsync(m => m.Id == paymentCodeMatch.MachineId);

                if (machine != null && !string.IsNullOrEmpty(machine.ZigbeeNetworkId))
                {
                    // Tính toán số pulses (mặc định 1 pulse cho test, hoặc tính theo logic của bạn)
                    // Ở đây tạm dùng 1 pulse cho Washer, và số bước cho Dryer
                    int pulses = 1;
                    if (machine.Type == MachineType.Dryer)
                    {
                        // Giả sử mỗi 10 phút là 1 pulse
                        pulses = Math.Max(1, paymentCodeMatch.TotalMinutes / 10);
                    }

                    string? storeCode = machine.Store?.StoreId;
                    await _zigbeeService.TriggerAsync(machine.ZigbeeNetworkId, pulses, storeCode);
                    _logger.LogInformation("[SePay Webhook] Triggered Zigbee machine: {Topic} with {Pulses} pulses in Store {StoreCode}", machine.ZigbeeNetworkId, pulses, storeCode);
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SePay Webhook] Error processing webhook for transaction {Id}", dto.Id);
                transaction.Status = "Error";
                await _context.SaveChangesAsync();
                // Trả về 500 để SePay có thể retry nếu là lỗi tạm thời (như MQTT timeout)
                return StatusCode(500);
            }
        }

        private static string? ExtractPaymentCode(string? content)
        {
            var match = Regex.Match(content ?? string.Empty, @"\bQLS[A-F0-9]{5}\b", RegexOptions.IgnoreCase);
            return match.Success ? match.Value.ToUpperInvariant() : null;
        }

        private static bool TryParseFreshTimestamp(string value, out DateTimeOffset timestamp)
        {
            timestamp = default;
            if (long.TryParse(value, out var unixTimestamp))
            {
                timestamp = unixTimestamp > 9_999_999_999
                    ? DateTimeOffset.FromUnixTimeMilliseconds(unixTimestamp)
                    : DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
            }
            else if (!DateTimeOffset.TryParse(value, out timestamp))
            {
                return false;
            }

            return Math.Abs((DateTimeOffset.UtcNow - timestamp.ToUniversalTime()).TotalMinutes) <= 5;
        }
    }
}
