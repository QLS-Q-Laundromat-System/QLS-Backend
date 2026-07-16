using Microsoft.AspNetCore.Authorization;
using QLS.Backend.Data;
using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs;
using QLS.Backend.DTOs.Machine;
using QLS.Backend.Interfaces;
using QLS.Backend.Extensions;

namespace QLS.Backend.Controllers;

/// <summary>
/// API quản lý cấu hình (setting) cho từng máy.
/// Base route: api/machines/{machineId}/setting
/// </summary>
[ApiController]
[Route("api/machines/{machineId:guid}/setting")]
[Authorize(Roles = "SystemAdmin,BrandAdmin,Manager")]
public class MachineSettingController : ControllerBase
{
    private readonly IMachineSettingService _settingService;
    private readonly ILgMachineSettingSyncService _syncService;
    private readonly AppDbContext _context;

    public MachineSettingController(
        IMachineSettingService settingService,
        ILgMachineSettingSyncService syncService,
        AppDbContext context)
    {
        _settingService = settingService;
        _syncService = syncService;
        _context = context;
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
        await User.EnsureMachineAccessAsync(_context, machineId);
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
        await User.EnsureMachineAccessAsync(_context, machineId);
        var result = await _syncService.UpdateAndSyncSettingAsync(machineId, dto);
        return Ok(ApiResponse<MachineSettingDto>.Success(result, "Lưu và đồng bộ cấu hình máy thành công."));
    }

    // DELETE api/machines/{machineId}/setting
    /// <summary>Xoá cấu hình của máy.</summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(Guid machineId)
    {
        await User.EnsureMachineAccessAsync(_context, machineId);
        var deleted = await _settingService.DeleteAsync(machineId);

        if (!deleted)
            return NotFound(ApiResponse<object>.Error(404, "Không tìm thấy cấu hình để xoá."));

        return Ok(ApiResponse<object>.Success(new { }, "Xoá cấu hình máy thành công."));
    }
}
