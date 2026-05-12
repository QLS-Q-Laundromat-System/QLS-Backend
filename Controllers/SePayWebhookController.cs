using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.DTOs.Integrations;
using QLS.Backend.Interfaces;
using QLS.Backend.Models;
using QLS.Backend.Models.Enums;
using QLS.Backend.Services;
using System.Text.Json;

namespace QLS.Backend.Controllers
{
    [ApiController]
    [Route("api/webhooks/sepay")]
    public class SePayWebhookController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IDryerService _dryerService;
        private readonly IZigbeeService _zigbeeService;
        private readonly ILogger<SePayWebhookController> _logger;

        public SePayWebhookController(
            AppDbContext context,
            IDryerService dryerService,
            IZigbeeService zigbeeService,
            ILogger<SePayWebhookController> logger)
        {
            _context = context;
            _dryerService = dryerService;
            _zigbeeService = zigbeeService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Receive([FromBody] SePayWebhookDto dto)
        {
            _logger.LogInformation("[SePay Webhook] Received: {Content} | Amount: {Amount}", dto.Content, dto.TransferAmount);

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
                    transaction.Status = "Ignored (Not Inbound)";
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true });
                }

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
