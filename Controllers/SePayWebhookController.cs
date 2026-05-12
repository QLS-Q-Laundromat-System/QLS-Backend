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
            _logger.LogInformation("[SePay Webhook] Received: {Id} | Content: {Content} | Amount: {Amount}", dto.Id, dto.Content, dto.TransferAmount);

            // 0. Kiểm tra chống trùng lặp (Idempotency)
            // Tìm giao dịch đã xử lý trước đó
            var existingTransaction = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t => t.GatewayTransactionId == dto.Id.ToString());

            if (existingTransaction != null && existingTransaction.Status == "Success")
            {
                _logger.LogInformation("[SePay Webhook] Transaction {Id} already processed successfully. Skipping.", dto.Id);
                return Ok(new { success = true });
            }

            // 1. Tạo hoặc lấy bản ghi giao dịch (Audit Log)
            var transaction = existingTransaction;
            if (transaction == null)
            {
                transaction = new PaymentTransaction
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
                await _context.SaveChangesAsync();
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

                // 2. Tìm mã thanh toán trong nội dung chuyển khoản (VD: QLS12345)
                var content = dto.Content ?? "";
                _logger.LogInformation("[SePay Webhook] Searching for session with payment code in content: {Content}", content);
                
                // Tìm session dựa trên PaymentCode. Chấp nhận cả PendingPayment và Running (để retry)
                var paymentCodeMatch = _context.MachineSessions
                    .Where(s => s.Status == MachineSessionStatus.PendingPayment || s.Status == MachineSessionStatus.Running)
                    .AsEnumerable() 
                    .FirstOrDefault(s => !string.IsNullOrEmpty(s.PaymentCode) && content.ToUpper().Contains(s.PaymentCode.ToUpper()));

                if (paymentCodeMatch == null)
                {
                    _logger.LogWarning("[SePay Webhook] No matching PendingPayment/Running session found for content: {Content}", content);
                    transaction.Status = "Failed (No Match)";
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true }); 
                }

                _logger.LogInformation("[SePay Webhook] Found session {SessionId} for machine {MachineId}", paymentCodeMatch.Id, paymentCodeMatch.MachineId);

                // 3. Kiểm tra số tiền
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
                if (paymentCodeMatch.Status == MachineSessionStatus.PendingPayment)
                {
                    _logger.LogInformation("[SePay Webhook] Confirming payment for session {SessionId}", paymentCodeMatch.Id);
                    await _dryerService.ConfirmPaymentAsync(paymentCodeMatch.Id, dto.Id.ToString());
                }
                else
                {
                    _logger.LogInformation("[SePay Webhook] Session {SessionId} is already Running, proceeding to trigger machine.", paymentCodeMatch.Id);
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

                if (string.IsNullOrEmpty(machine.ZigbeeNetworkId))
                {
                    _logger.LogWarning("[SePay Webhook] Machine {MachineName} ({MachineId}) has no ZigbeeNetworkId. Cannot trigger coins!", machine.Name, machine.Id);
                    transaction.Status = "Success (No Zigbee ID)";
                }
                else
                {
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

                    _logger.LogInformation("[SePay Webhook] TRIGGERING ZIGBEE: Topic={Topic}, Pulses={Pulses}", machine.ZigbeeNetworkId, pulses);
                    await _zigbeeService.TriggerAsync(machine.ZigbeeNetworkId, pulses);
                    _logger.LogInformation("[SePay Webhook] Zigbee trigger sent successfully.");
                    transaction.Status = "Success";
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
