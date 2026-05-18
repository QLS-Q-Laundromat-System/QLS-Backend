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

            _logger.LogInformation("[SePay Webhook] Received: {Id} | Content: {Content} | Amount: {Amount}", dto.Id, dto.Content, dto.TransferAmount);

            // Lấy cấu hình Secret/Token từ DB dựa trên AccountNumber
            string? webhookSecret = _configuration["SePay:WebhookSecret"];
            string? secretToken = _configuration["SePay:WebhookToken"];

            if (!string.IsNullOrEmpty(dto.AccountNumber))
            {
                var paymentConfig = await _context.PaymentConfigs
                    .FirstOrDefaultAsync(p => p.AccountNumber == dto.AccountNumber && p.IsActive && p.Provider == "SEPAY");
                
                if (paymentConfig != null)
                {
                    if (!string.IsNullOrEmpty(paymentConfig.SecretKey)) webhookSecret = paymentConfig.SecretKey;
                    if (!string.IsNullOrEmpty(paymentConfig.ApiKey)) secretToken = paymentConfig.ApiKey;
                }
            }

            // 1. Xác thực HMAC-SHA256 (Khuyến nghị từ SePay)
            var signatureHeader = Request.Headers["X-SePay-Signature"].ToString();
            var timestampHeader = Request.Headers["X-SePay-Timestamp"].ToString();

            if (!string.IsNullOrEmpty(webhookSecret) && !string.IsNullOrEmpty(signatureHeader))
            {
                try 
                {
                    var dataToSign = $"{timestampHeader}.{rawBody}";
                    var keyBytes = Encoding.UTF8.GetBytes(webhookSecret);
                    var dataBytes = Encoding.UTF8.GetBytes(dataToSign);

                    using var hmac = new HMACSHA256(keyBytes);
                    var hashBytes = hmac.ComputeHash(dataBytes);
                    var computedHash = Convert.ToHexString(hashBytes).ToLower();

                    var expectedSignature = $"sha256={computedHash}";
                    if (!string.Equals(signatureHeader, expectedSignature, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("[SePay Webhook] INVALID HMAC SIGNATURE! Header: {Header}, Expected: {Expected}", signatureHeader, expectedSignature);
                        return Unauthorized(new { message = "Invalid HMAC Signature" });
                    }
                    _logger.LogInformation("[SePay Webhook] HMAC Signature verified successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SePay Webhook] Error verifying HMAC signature.");
                    return Unauthorized(new { message = "Signature verification error" });
                }
            }
            else
            {
                // Fallback: Kiểm tra API Key Token nếu không dùng HMAC
                var authHeader = Request.Headers["Authorization"].ToString();

                if (string.IsNullOrEmpty(authHeader) || (!string.IsNullOrEmpty(secretToken) && !authHeader.Contains(secretToken)))
                {
                    _logger.LogWarning("[SePay Webhook] UNAUTHORIZED access attempt (API Key)! Header: {Header}", authHeader);
                    return Unauthorized(new { message = "Invalid API Token" });
                }
            }

            // 0.1 Tìm mã thanh toán trong nội dung chuyển khoản
            var content = dto.Content ?? "";
            var paymentCodeMatch = _context.MachineSessions
                .Where(s => s.Status == MachineSessionStatus.PendingPayment || s.Status == MachineSessionStatus.PaidWaitingForStart || s.Status == MachineSessionStatus.Running)
                .AsEnumerable() 
                .FirstOrDefault(s => !string.IsNullOrEmpty(s.PaymentCode) && content.ToUpper().Contains(s.PaymentCode.ToUpper()));

            if (paymentCodeMatch == null)
            {
                _logger.LogWarning("[SePay Webhook] No matching session found for content: {Content}", content);
                return Ok(new { success = true }); 
            }

            return await ProcessPaymentAsync(dto, paymentCodeMatch);
        }

        /// <summary>
        /// API Sandbox để test local: POST /api/webhooks/sepay/test-pay
        /// </summary>
        [HttpPost("test-pay")]
        public async Task<IActionResult> TestPayment([FromBody] SePayTestRequest request)
        {
            // Bảo vệ: Chỉ cho phép chạy Sandbox ở môi trường Development (Local)
            if (!_env.IsDevelopment())
            {
                _logger.LogWarning("[Sandbox] Tu choi yeu cau TestPayment vi khong phai moi truong Development.");
                return Forbid("Tinh nang nay chi danh cho moi truong Local.");
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
            bool isDuplicate = await _context.PaymentTransactions
                .AnyAsync(t => t.GatewayTransactionId == dto.Id.ToString());

            if (isDuplicate)
            {
                _logger.LogInformation("[SePay Webhook] Transaction {Id} already processed. Ignoring.", dto.Id);
                return Ok(new { success = true });
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
                Status = "Pending"
            };

            _context.PaymentTransactions.Add(transaction);
            await _context.SaveChangesAsync();

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
                var machine = await _context.Machines.FindAsync(paymentCodeMatch.MachineId);
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

                    await _zigbeeService.TriggerAsync(machine.ZigbeeNetworkId, pulses);
                    _logger.LogInformation("[SePay Webhook] Triggered Zigbee machine: {Topic} with {Pulses} pulses", machine.ZigbeeNetworkId, pulses);
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
    }
}
