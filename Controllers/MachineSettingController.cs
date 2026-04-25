using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs;
using QLS.Backend.DTOs.Machine;
using QLS.Backend.Interfaces;

namespace QLS.Backend.Controllers;

/// <summary>
/// API quản lý cấu hình (setting) cho từng máy.
/// Base route: api/machines/{machineId}/setting
/// </summary>
[ApiController]
[Route("api/machines/{machineId:guid}/setting")]
public class MachineSettingController : ControllerBase
{
    private readonly IMachineSettingService _settingService;
    private readonly ILgMachineSettingSyncService _syncService;

    public MachineSettingController(
        IMachineSettingService settingService,
        ILgMachineSettingSyncService syncService)
    {
        _settingService = settingService;
        _syncService = syncService;
    }

    // GET api/machines/{machineId}/setting
    /// <summary>
    /// Lấy cấu hình của máy một cách thông minh:
    /// - Nếu DB đã có setting → trả về ngay (không gọi LG).
    /// - Nếu chưa có → tự động gọi LG API, lưu vào DB rồi trả về.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(Guid machineId)
    {
        var result = await _syncService.GetOrFetchSettingAsync(machineId);
        return Ok(ApiResponse<MachineSettingDto>.Success(result, "Lấy cấu hình máy thành công."));
    }

    // PUT api/machines/{machineId}/setting
    /// <summary>
    /// Tạo mới hoặc ghi đè toàn bộ cấu hình của máy (Upsert).
    /// Nếu chưa có setting → tạo mới.
    /// Nếu đã có setting → thay thế hoàn toàn.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Upsert(Guid machineId, [FromBody] UpsertMachineSettingDto dto)
    {
        var result = await _settingService.UpsertAsync(machineId, dto);
        return Ok(ApiResponse<MachineSettingDto>.Success(result, "Lưu cấu hình máy thành công."));
    }

    // DELETE api/machines/{machineId}/setting
    /// <summary>Xoá cấu hình của máy.</summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(Guid machineId)
    {
        var deleted = await _settingService.DeleteAsync(machineId);

        if (!deleted)
            return NotFound(ApiResponse<object>.Error(404, "Không tìm thấy cấu hình để xoá."));

        return Ok(ApiResponse<object>.Success(new { }, "Xoá cấu hình máy thành công."));
    }
}
