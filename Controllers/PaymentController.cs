using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs.Zigbee;
using QLS.Backend.Interfaces;
using QLS.Backend.Services;
using System;
using System.Threading.Tasks;

namespace QLS.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IZigbeeService _zigbeeService;
        private readonly IDryerService _dryerService;

        public PaymentController(IZigbeeService zigbeeService, IDryerService dryerService)
        {
            _zigbeeService = zigbeeService;
            _dryerService = dryerService;
        }

        /// <summary>
        /// [GET] /pulse/{count}
        /// API test siêu tốc. Vừa nhả xu, vừa hỗ trợ lưu DB nếu truyền thêm param.
        /// </summary>
        [HttpGet("/pulse/{count}")]
        public async Task<ActionResult<TriggerWasherResponseDto>> TriggerPulse(
            int count, 
            [FromQuery] string topic = "QLS.Washer",
            [FromQuery] Guid? branchId = null,
            [FromQuery] string? machineId = null,
            [FromQuery] Guid? userId = null,
            [FromQuery] int? minutes = null)
        {
            Console.WriteLine($"[DIAGNOSTIC] EXECUTING TRIGGER: topic={topic}, count={count}");

            // 1. LƯU DATABASE (Chỉ chạy khi FE truyền đủ thông tin)
            if (branchId.HasValue && machineId != null && userId.HasValue && minutes.HasValue)
            {
                await _dryerService.SaveSessionAsync(branchId.Value, machineId, userId.Value, minutes.Value);
                Console.WriteLine($"[DB] Đã lưu lịch sử sấy: User {userId}, {minutes} phút.");
            }

            // 2. KÍCH HOẠT ESP32 NHẢ XU
            await _zigbeeService.TriggerAsync(topic, count);

            return Ok(new TriggerWasherResponseDto
            {
                Success = true,
                Message = $"Đã gửi lệnh nhả {count} xu tới máy '{topic}'.",
                ZigbeeDeviceTopic = topic,
                BagCount = count
            });
        }
    }
}
