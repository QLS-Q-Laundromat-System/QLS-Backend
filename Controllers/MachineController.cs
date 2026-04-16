using Microsoft.AspNetCore.Mvc;
using QLS.Backend.Services;
using QLS.Backend.DTOs;
using QLS.Backend.Services.LgService;

namespace QLS.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MachineController : ControllerBase
{
    private readonly IMachineDetailService _machineDetailService;

    // Chỉ Inject duy nhất Service mà Sơn đang có
    public MachineController(IMachineDetailService machineDetailService)
    {
        _machineDetailService = machineDetailService;
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
    public async Task<IActionResult> UpdateCapacity(Guid id, [FromBody] QLS.Backend.DTOs.Machine.UpdateMachineCapacityDto dto)
    {
        var result = await _machineDetailService.UpdateMachineCapacityAsync(id, dto.Capacity);
        if (!result) return NotFound(new { message = "Không tìm thấy máy" });
        
        return Ok(new { message = "Cập nhật công suất máy thành công" });
    }
}