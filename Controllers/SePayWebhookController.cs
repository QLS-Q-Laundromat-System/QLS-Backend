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
        private readonly ILogger<SePayWebhookController> _logger;

        public SePayWebhookController(
            AppDbContext context,
            IMachineService machineService,
            IZigbeeService zigbeeService,
            IConfiguration configuration,
            IHostEnvironment env,
            IHardwareTrackerService hardwareTracker,
            ILogger<SePayWebhookController> logger)
        {
            _context = context;
            _machineService = machineService;
            _zigbeeService = zigbeeService;
            _configuration = configuration;
            _env = env;
            _hardwareTracker = hardwareTracker;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Receive()
        {
<<<<<<< Updated upstream
            _logger.LogInformation("[SePay Webhook] Received: {Content} | Amount: {Amount}", dto.Content, dto.TransferAmount);
=======
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
>>>>>>> Stashed changes

            // 1. Xác thực HMAC-SHA256 (Khuyến nghị từ SePay)
            var signatureHeader = Request.Headers["X-SePay-Signature"].ToString();
            var timestampHeader = Request.Headers["X-SePay-Timestamp"].ToString();
            var webhookSecret = _configuration["SePay:WebhookSecret"];

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
                var secretToken = _configuration["SePay:WebhookToken"];

                if (string.IsNullOrEmpty(authHeader) || !authHeader.Contains(secretToken))
                {
                    _logger.LogWarning("[SePay Webhook] UNAUTHORIZED access attempt (API Key)! Header: {Header}", authHeader);
                    return Unauthorized(new { message = "Invalid API Token" });
                }
            }

            // 0.1 Tìm mã thanh toán trong nội dung chuyển khoản
            var content = dto.Content ?? "";
            var paymentCodeMatch = _context.MachineSessions
                .Where(s => s.Status == MachineSessionStatus.PendingPayment || s.Status == MachineSessionStatus.PaidWaitingForStart)
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
                                        (s.Status == MachineSessionStatus.PendingPayment || s.Status == MachineSessionStatus.PaidWaitingForStart));

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
<<<<<<< Updated upstream
            // Nếu webhook được gọi lại với cùng 1 ID giao dịch, ta bỏ qua và trả về thành công
            bool isDuplicate = await _context.PaymentTransactions
                .AnyAsync(t => t.GatewayTransactionId == dto.Id.ToString());
=======
            var existingTransaction = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t => t.GatewayTransactionId == dto.Id.ToString());
>>>>>>> Stashed changes

            if (isDuplicate)
            {
<<<<<<< Updated upstream
                _logger.LogInformation("[SePay Webhook] Transaction {Id} already processed. Ignoring.", dto.Id);
                return Ok(new { success = true });
            }

            // 1. Tạo bản ghi giao dịch (Audit Log)
            var transaction = new PaymentTransaction
=======
                return Ok(new { success = true });
            }

            // 1. Tạo hoặc lấy bản ghi giao dịch
            var transaction = existingTransaction;
            if (transaction == null)
>>>>>>> Stashed changes
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
                    transaction.Status = "Ignored (Not Inbound)";
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true });
                }

<<<<<<< Updated upstream
                // 2. Tìm mã thanh toán trong nội dung chuyển khoản (VD: QLS12345)
                // SePay thường để content chứa mã chúng ta yêu cầu khách nhập.
                var content = dto.Content ?? "";
                var paymentCodeMatch = _context.MachineSessions
                    .Where(s => s.Status == MachineSessionStatus.PendingPayment)
                    .AsEnumerable() // Chuyển sang xử lý trên memory để dùng string.Contains hoặc Regex nếu cần
                    .FirstOrDefault(s => !string.IsNullOrEmpty(s.PaymentCode) && content.ToUpper().Contains(s.PaymentCode.ToUpper()));

                if (paymentCodeMatch == null)
                {
                    _logger.LogWarning("[SePay Webhook] No matching PendingPayment session found for content: {Content}", content);
                    transaction.Status = "Failed (No Match)";
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true }); // Vẫn trả về success=true để SePay không gửi lại
                }

                // 3. Kiểm tra số tiền (Cho phép sai số nhỏ nếu cần, hoặc khớp chính xác)
=======
                _logger.LogInformation("[Process] Found session {SessionId} for machine {MachineId}", paymentCodeMatch.Id, paymentCodeMatch.MachineId);

                // 3. Kiểm tra số tiền
>>>>>>> Stashed changes
                if (dto.TransferAmount < paymentCodeMatch.PricePaid)
                {
                    _logger.LogWarning("[SePay Webhook] Amount mismatch. Expected: {Expected}, Received: {Received}", paymentCodeMatch.PricePaid, dto.TransferAmount);
                    transaction.Status = "MismatchAmount";
                    transaction.MachineSessionId = paymentCodeMatch.Id;
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true });
                }

                // 4. Xác nhận thanh toán và kích hoạt máy
                transaction.MachineSessionId = paymentCodeMatch.Id;
<<<<<<< Updated upstream
                transaction.Status = "Success";
                
                // Cập nhật trạng thái session sang Running
                await _dryerService.ConfirmPaymentAsync(paymentCodeMatch.Id, dto.Id.ToString());

                // Kích hoạt ESP32 qua Zigbee
                // Lấy topic từ Machine.ZigbeeNetworkId
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
=======
                if (paymentCodeMatch.Status == MachineSessionStatus.PendingPayment)
                {
                    _logger.LogInformation("[SePay Webhook] Confirming payment for session {SessionId}", paymentCodeMatch.Id);
                    await _machineService.ConfirmPaymentAsync(paymentCodeMatch.Id, dto.Id.ToString());
                }
                else
                {
                    _logger.LogInformation("[SePay Webhook] Session {SessionId} is already PaidWaitingForStart, proceeding to trigger machine.", paymentCodeMatch.Id);
                }

                // 5. Kích hoạt ESP32 qua Zigbee
                var machine = await _context.Machines.FindAsync(paymentCodeMatch.MachineId);
                if (machine == null)
                {
                    _logger.LogError("[SePay Webhook] Machine {MachineId} not found for session {SessionId}", paymentCodeMatch.MachineId, paymentCodeMatch.Id);
                    transaction.Status = "Error (Machine Not Found)";
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true });
                }

                // Sử dụng ZigbeeNetworkId từ máy, nếu không có thì fallback về "QLS.Washer" như PaymentController
                string zigbeeTopic = !string.IsNullOrEmpty(machine.ZigbeeNetworkId) 
                    ? machine.ZigbeeNetworkId 
                    : "QLS.Washer";

                if (string.IsNullOrEmpty(machine.ZigbeeNetworkId))
                {
                    _logger.LogWarning("[SePay Webhook] Machine {MachineName} ({MachineId}) has no ZigbeeNetworkId. Falling back to default: {DefaultTopic}", 
                        machine.Name, machine.Id, zigbeeTopic);
                }
                
                // Tính toán số pulses
                int pulses = 1;
                if (machine.Type == MachineType.Dryer)
                {
                    var capacityClean = new string(machine.Capacity.Where(c => char.IsDigit(c) || c == '.').ToArray());
                    decimal.TryParse(capacityClean, out var cap);

                    var dryerPriceMode = await _context.PriceModePerSessions
                        .OfType<DryerPriceMode>()
                        .FirstOrDefaultAsync(m => m.PriceListId == paymentCodeMatch.PriceListId && 
                                                m.MachineCapacityKg == cap);

                    if (dryerPriceMode != null && dryerPriceMode.DurationMinutes > 0)
                    {
                        pulses = paymentCodeMatch.TotalMinutes / dryerPriceMode.DurationMinutes;
                        _logger.LogInformation("[SePay Webhook] Calculated {Pulses} pulses for Dryer ({TotalMin}/{StepMin})", 
                            pulses, paymentCodeMatch.TotalMinutes, dryerPriceMode.DurationMinutes);
                    }
                    else
                    {
                        _logger.LogWarning("[SePay Webhook] Could not find DryerPriceMode for capacity {Cap}. Defaulting to 1 pulse.", cap);
                    }
                }
                else
                {
                    _logger.LogInformation("[SePay Webhook] Machine is Washer, using default 1 pulse.");
                }

                _logger.LogInformation("[SePay Webhook] TRIGGERING ZIGBEE: Topic={Topic}, Pulses={Pulses}", zigbeeTopic, pulses);
                
                try 
                {
                    // Gửi lệnh bắn xu
                    var zigbeeId = string.IsNullOrEmpty(machine.ZigbeeNetworkId) ? "QLS.Washer" : machine.ZigbeeNetworkId;
                    _logger.LogInformation("[SePay Webhook] Sending pulse to Zigbee ID: {ZigbeeId}", zigbeeId);
                    
                    _hardwareTracker.UpdateStatus(paymentCodeMatch.Id, $"Đang kết nối tới thiết bị {zigbeeId} ({pulses} lần bắn xu)...");
                    await _zigbeeService.TriggerAsync(zigbeeId, pulses);
                    _hardwareTracker.UpdateStatus(paymentCodeMatch.Id, "Đã gửi lệnh kích hoạt máy.");

                    _logger.LogInformation("[SePay Webhook] Zigbee trigger sent successfully.");
                    transaction.Status = "Success";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SePay Webhook] FAILED to trigger Zigbee for session {SessionId}. Machine might be offline or MQTT Broker error.", paymentCodeMatch.Id);
                    
                    // Cập nhật trạng thái Session thành Error để thông báo cho người dùng
                    await _machineService.UpdateSessionStatusAsync(paymentCodeMatch.Id, MachineSessionStatus.Error, "Lỗi kết nối thiết bị bắn xu. Vui lòng liên hệ quản lý.");
                    
                    transaction.Status = "Error (Trigger Failed)";
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true }); // Trả về Ok cho SePay để không retry, nhưng máy đã lỗi
>>>>>>> Stashed changes
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SePay Webhook] Error processing webhook");
                transaction.Status = "Error";
                await _context.SaveChangesAsync();
                return StatusCode(500);
            }
        }
    }
}
