using QLS.Backend.Data;
using QLS.Backend.DTOs;
using QLS.Backend.Integrations.LG;
using QLS.Backend.Models;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Services;

public class MachineDetailService : IMachineDetailService
{
    private readonly LgApiClient _lgClient;
    private readonly AppDbContext _context;

    public MachineDetailService(LgApiClient lgClient, AppDbContext context)
    {
        _lgClient = lgClient;
        _context = context;
    }

    public async Task<List<MachineDetailDto>> GetLgMachineStatusAsync(Guid branchId)
    {
        // 1. Gọi API lấy dữ liệu thô (LgClient đang nhận string nên ta .ToString())
        var rawJson = await _lgClient.GetRawStatusAsync(branchId.ToString());
        
        // 2. Chế biến dữ liệu sang DTO
        var statusList = LgMapper.MapToDto(rawJson);

        // 3. Logic: Tự động cập nhật Database
        var existingIds = await _context.Machines.Select(m => m.MachineId).ToHashSetAsync();
        var newMachines = statusList
            .Where(s => !existingIds.Contains(s.DeviceId))
            .Select(s => new Machine {
                MachineId = s.DeviceId,
                BranchId = branchId,
                Type = s.DeviceType == "0" ? MachineType.Giat : MachineType.Say,
                Capacity = "LG_COMMERCIAL"
            }).ToList();

        if (newMachines.Any())
        {
            _context.Machines.AddRange(newMachines);
            await _context.SaveChangesAsync();
        }

        return statusList;
    }
}