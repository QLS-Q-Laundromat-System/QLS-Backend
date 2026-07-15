using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.Models;
using QLS.Backend.Services;
using QLS.Backend.DTOs;
using QLS.Backend.DTOs.Machine;
using QLS.Backend.Services.LgService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QLS.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MachineController : ControllerBase
{
    private readonly IMachineDetailService _machineDetailService;
    private readonly AppDbContext _context;
    private readonly IZigbeeService _zigbeeService;

    public MachineController(
        IMachineDetailService machineDetailService,
        AppDbContext context,
        IZigbeeService zigbeeService)
    {
        _machineDetailService = machineDetailService;
        _context = context;
        _zigbeeService = zigbeeService;
    }

    // API lấy trạng thái trực tiếp từ LG
    [HttpGet("status/{storeId}")]
    public async Task<IActionResult> GetLgStatus(string storeId)
    {
        var result = await _machineDetailService.GetLgMachineStatusAsync(storeId);
        return Ok(ApiResponse<IEnumerable<MachineDetailDto>>.Success(result, "Lấy trạng thái máy thành công"));
    }

    // API Cập nhật công suất (số kg) của máy
    [HttpPatch("{id}/capacity")]
    // [Authorize(Roles = "SystemAdmin,BrandAdmin,StoreAdmin")]
    public async Task<IActionResult> UpdateCapacity(Guid id, [FromBody] UpdateMachineCapacityDto dto)
    {
        var result = await _machineDetailService.UpdateMachineCapacityAsync(id, dto.Capacity);
        if (!result) return NotFound(new { message = "Không tìm thấy máy" });
        
        return Ok(new { message = "Cập nhật công suất máy thành công" });
    }

    // API lấy chi tiết máy với cấu hình + bảng giá
    [HttpGet("{id}/detail")]
    public async Task<IActionResult> GetMachineDetail(Guid id)
    {
        var result = await _machineDetailService.GetMachineDetailWithConfigAsync(id);
        if (result == null) return NotFound(new { message = "Không tìm thấy máy" });

        return Ok(ApiResponse<MachineDetailWithConfigDto>.Success(result, "Lấy chi tiết máy thành công"));
    }

    // API Thiết lập địa chỉ Zigbee và gửi lệnh đổi tên qua MQTT
    [HttpPost("setup-zigbee")]
    public async Task<IActionResult> SetupZigbee([FromBody] SetupZigbeeRequestDto dto)
    {
        var machine = await _context.Machines
            .Include(m => m.Store)
            .FirstOrDefaultAsync(m => m.Id == dto.MachineId);

        if (machine == null)
        {
            return NotFound(ApiResponse<object>.Error(404, "Không tìm thấy máy giặt/sấy."));
        }

        var storeCode = machine.Store?.StoreId;
        if (string.IsNullOrEmpty(storeCode))
        {
            return BadRequest(ApiResponse<object>.Error(400, "Cửa hàng liên kết chưa có cấu hình mã định danh (StoreId)."));
        }

        // Cập nhật thông tin vào DB
        machine.Esp32MacAddress = dto.IeeeAddress;
        machine.ZigbeeNetworkId = dto.FriendlyName;
        
        await _context.SaveChangesAsync();

        // Gửi lệnh đổi tên tới Zigbee2MQTT
        try
        {
            await _zigbeeService.RenameDeviceAsync(storeCode, dto.IeeeAddress, dto.FriendlyName);
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<object>.Success(
                new { machineId = machine.Id, friendlyName = dto.FriendlyName }, 
                $"Đã lưu cấu hình vào DB nhưng gặp lỗi gửi lệnh đổi tên MQTT: {ex.Message}"));
        }

        return Ok(ApiResponse<object>.Success(
            new { machineId = machine.Id, friendlyName = dto.FriendlyName }, 
            "Cấu hình thiết bị Zigbee và gửi lệnh đổi tên thành công."));
    }

    [HttpGet("discovered-devices/{machineId}")]
    public async Task<IActionResult> GetDiscoveredDevices(Guid machineId)
    {
        var machine = await _context.Machines
            .Include(m => m.Store)
            .FirstOrDefaultAsync(m => m.Id == machineId);

        if (machine == null)
        {
            return NotFound(ApiResponse<object>.Error(404, "Không tìm thấy máy."));
        }

        var storeCode = machine.Store?.StoreId;
        if (string.IsNullOrEmpty(storeCode))
        {
            return BadRequest(ApiResponse<object>.Error(400, "Cửa hàng liên kết chưa cấu hình StoreId."));
        }

        // Lấy danh sách thiết bị quét được từ cache MQTT
        if (!QLS.Backend.Services.Ziggbee.MqttListenerService.DiscoveredDevices.TryGetValue(storeCode, out var devices))
        {
            devices = new List<QLS.Backend.Services.Ziggbee.MqttListenerService.DiscoveredZigbeeDevice>();
        }

        // Lọc ra các thiết bị chưa được gán cho bất kỳ máy nào khác
        var assignedMacs = await _context.Machines
            .Where(m => m.Id != machineId && !string.IsNullOrEmpty(m.Esp32MacAddress))
            .Select(m => m.Esp32MacAddress)
            .ToListAsync();

        var unassignedDevices = devices
            .Where(d => !assignedMacs.Contains(d.IeeeAddress))
            .ToList();

        return Ok(ApiResponse<IEnumerable<QLS.Backend.Services.Ziggbee.MqttListenerService.DiscoveredZigbeeDevice>>.Success(unassignedDevices, "Lấy danh sách thiết bị thành công"));
    }

    [HttpPost("permit-join")]
    public async Task<IActionResult> SetPermitJoin([FromBody] PermitJoinRequestDto dto)
    {
        var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == dto.StoreId);
        if (store == null)
        {
            return NotFound(ApiResponse<object>.Error(404, "Không tìm thấy cửa hàng."));
        }

        var storeCode = store.StoreId;
        if (string.IsNullOrEmpty(storeCode))
        {
            return BadRequest(ApiResponse<object>.Error(400, "Cửa hàng chưa có cấu hình mã định danh (StoreId)."));
        }

        try
        {
            await _zigbeeService.SetPermitJoinAsync(storeCode, dto.Permit, dto.DurationSeconds);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Error(500, $"Lỗi khi phát lệnh MQTT permit_join: {ex.Message}"));
        }

        return Ok(ApiResponse<object>.Success(
            new { permit = dto.Permit, duration = dto.DurationSeconds }, 
            dto.Permit ? $"Đã mở mạng ghép đôi thiết bị trong {dto.DurationSeconds} giây." : "Đã khóa mạng ghép đôi thiết bị."));
    }
}