using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs.Zigbee;
using QLS.Backend.Interfaces;
using QLS.Backend.Services;
using QLS.Backend.Models.Enums;
using QLS.Backend.DTOs.Machine;
using System;
using System.Threading.Tasks;

namespace QLS.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IZigbeeService _zigbeeService;
        private readonly IMachineService _machineService;

        public PaymentController(IZigbeeService zigbeeService, IMachineService machineService)
        {
            _zigbeeService = zigbeeService;
            _machineService = machineService;
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
            [FromQuery] Guid? machineId = null,
            [FromQuery] Guid? userId = null,
            [FromQuery] int? minutes = null,
            [FromQuery] decimal pricePaid = 0,
            [FromQuery] Guid? priceListId = null,
            [FromQuery] PricePerType pricingMode = PricePerType.Flat,
            [FromQuery] decimal? weightKg = null,
            [FromQuery] string? cycleName = null,
            [FromQuery] bool isExtension = false)
        {
            Console.WriteLine($"[DIAGNOSTIC] EXECUTING TRIGGER: topic={topic}, count={count}");

            // 1. LƯU DATABASE (Chỉ chạy khi FE truyền đủ thông tin)
            if (branchId.HasValue && machineId.HasValue && userId.HasValue && minutes.HasValue)
            {
                var sessionDto = new CreateMachineSessionDto
                {
                    BranchId = branchId.Value,
                    MachineId = machineId.Value,
                    UserId = userId.Value,
                    TotalMinutes = minutes.Value,
                    PricePaid = pricePaid,
                    PriceListId = priceListId,
                    PricingMode = pricingMode,
                    WeightKg = weightKg,
                    CycleName = cycleName,
                    IsExtension = isExtension
                };

                await _machineService.SaveSessionAsync(sessionDto);
                    
                Console.WriteLine($"[DB] Đã lưu lịch sử sấy: User {userId}, {minutes} phút, Giá: {pricePaid}.");
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
