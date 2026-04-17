using QLS.Backend.Data;
using QLS.Backend.DTOs;
using QLS.Backend.Services.LgService;
using QLS.Backend.Models;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Models.Enums;
using QLS.Backend.Interfaces.Brand;

namespace QLS.Backend.Services;

public class MachineDetailService : IMachineDetailService
{
    private readonly LgApiClient _lgClient;
    private readonly AppDbContext _context;
    private readonly IBrandLgService _brandLgService;

    public MachineDetailService(LgApiClient lgClient, AppDbContext context, IBrandLgService brandLgService)
    {
        _lgClient = lgClient;
        _context = context;
        _brandLgService = brandLgService;
    }

    public async Task<List<MachineDetailDto>> GetLgMachineStatusAsync(string storeId)
    {
        // 1. Phải lấy ra được chuỗi 'StoreId' (ID phía LG) từ Database
        var store = await _context.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StoreId == storeId);

        if (store == null)
        {
            throw new Exception("Không tìm thấy cửa hàng với mã LG StoreId này.");
        }

        var cred = await _brandLgService.GetValidCredentialAsync(store.BrandId);
        if (cred == null || string.IsNullOrEmpty(cred.LgUserNo) || string.IsNullOrEmpty(cred.AccessToken))
        {
            throw new Exception("Thương hiệu chưa được cấu hình tài khoản kết nối LG (LG Credential).");
        }

        // 2. Gọi API lấy dữ liệu thô bằng ID phía LG (hiện tại storeId truyền vào cũng chính là mã này)
        var rawJson = await _lgClient.GetRawStatusAsync(store.StoreId, cred.LgUserNo, cred.AccessToken);
        
        // 2. Chế biến dữ liệu sang DTO
        var statusList = LgMapper.MapToDto(rawJson);

        // 3. Logic: Tự động cập nhật Database
        var existingLgIds = await _context.Machines.Select(m => m.LgDeviceId).ToHashSetAsync();
        var newMachines = statusList
            .Where(s => !existingLgIds.Contains(s.DeviceId))
            .Select(s => new Machine {
                Id          = Guid.NewGuid(),
                LgDeviceId  = s.DeviceId,
                Name        = !string.IsNullOrEmpty(s.Alias) ? s.Alias : s.DeviceId, // Dùng Alias từ LG nếu có
                StoreId     = store.Id, // Lưu ý: Liên kết ForeignKey dùng Guid
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

    public async Task<bool> UpdateMachineCapacityAsync(Guid machineId, string capacity)
    {
        var machine = await _context.Machines.FirstOrDefaultAsync(m => m.Id == machineId);
        if (machine == null) return false;

        machine.Capacity = capacity;
        _context.Machines.Update(machine);
        await _context.SaveChangesAsync();
        
        return true;
    }
}