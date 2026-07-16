using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.Models;
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
    [Authorize(Roles = "SystemAdmin")]
    public class CoinController : ControllerBase
    {
        private readonly IZigbeeService _zigbeeService;
        private readonly IMachineService _machineService;
        private readonly AppDbContext _context;

        public CoinController(IZigbeeService zigbeeService, IMachineService machineService, AppDbContext context)
        {
            _zigbeeService = zigbeeService;
            _machineService = machineService;
            _context = context;
        }

        /// <summary>
        /// [GET] /pulse/{count}
        /// API bắn xu / kích hoạt nhả xu vật lý cho máy giặt/sấy qua Zigbee/MQTT.
        /// Hỗ trợ lưu thông tin phiên giặt xuống DB nếu FE truyền đầy đủ param.
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
            Console.WriteLine($"[DIAGNOSTIC] EXECUTING TRIGGER (BẮN XU): topic={topic}, count={count}");

            string? storeCode = null;
            if (machineId.HasValue)
            {
                var machine = await _context.Machines
                    .Include(m => m.Store)
                    .FirstOrDefaultAsync(m => m.Id == machineId.Value);
                if (machine?.Store != null)
                {
                    storeCode = machine.Store.StoreId;
                    if (string.IsNullOrEmpty(topic) || topic == "QLS.Washer")
                    {
                        topic = machine.ZigbeeNetworkId ?? topic;
                    }
                }
            }
            else if (branchId.HasValue)
            {
                var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == branchId.Value);
                if (store != null)
                {
                    storeCode = store.StoreId;
                }
            }

            // 1. LƯU DATABASE (Chỉ chạy khi FE truyền đủ thông tin phiên giặt)
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

            // 2. KÍCH HOẠT ESP32 NHẢ XU (BẮN XU)
            await _zigbeeService.TriggerAsync(topic, count, storeCode);

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

