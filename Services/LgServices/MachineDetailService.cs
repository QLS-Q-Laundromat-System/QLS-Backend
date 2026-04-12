using QLS.Backend.Data;
using QLS.Backend.DTOs;
using QLS.Backend.Services.LgService;
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

    public async Task<List<MachineDetailDto>> GetLgMachineStatusAsync(Guid storeId)
    {
        // 1. Gọi API lấy dữ liệu thô (LgClient đang nhận string nên ta .ToString())
        var rawJson = await _lgClient.GetRawStatusAsync(storeId.ToString());
        
        // 2. Chế biến dữ liệu sang DTO
        var statusList = LgMapper.MapToDto(rawJson);

        // 3. Logic: Tự động cập nhật Database
        var existingLgIds = await _context.Machines.Select(m => m.LgDeviceId).ToHashSetAsync();
        var newMachines = statusList
            .Where(s => !existingLgIds.Contains(s.DeviceId))
            .Select(s => new Machine {
                Id          = Guid.NewGuid(),
                LgDeviceId  = s.DeviceId,
                Name        = s.DeviceId, // Tên tạm – admin có thể đổi tên sau
                StoreId     = storeId,
                Type        = s.DeviceType == "0" ? MachineType.Washer : MachineType.Dryer,
                Capacity    = "LG_COMMERCIAL"
            }).ToList();

        if (newMachines.Any())
        {
            _context.Machines.AddRange(newMachines);
            await _context.SaveChangesAsync();
        }

        return statusList;
    }
}