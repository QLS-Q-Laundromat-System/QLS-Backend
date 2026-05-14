using QLS.Backend.Data;
using QLS.Backend.DTOs;
using QLS.Backend.DTOs.Machine;
using QLS.Backend.DTOs.Pricing;
using QLS.Backend.Services.LgService;
using QLS.Backend.Models;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Models.Enums;
using QLS.Backend.Interfaces.Brand;
using QLS.Backend.Interfaces.Pricing;

namespace QLS.Backend.Services;

public class MachineDetailService : IMachineDetailService
{
    private readonly LgApiClient _lgClient;
    private readonly AppDbContext _context;
    private readonly IBrandLgService _brandLgService;
    private readonly IPricingService _pricingService;

    public MachineDetailService(
        LgApiClient lgClient,
        AppDbContext context,
        IBrandLgService brandLgService,
        IPricingService pricingService)
    {
        _lgClient = lgClient;
        _context = context;
        _brandLgService = brandLgService;
        _pricingService = pricingService;
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
        var rawJson = await _lgClient.GetRawStatusAsync(store.StoreId!, cred.LgUserNo, cred.AccessToken);


        // 2. Chế biến dữ liệu sang DTO
        var statusList = LgMapper.MapToDto(rawJson);

        // 3. Logic: Tự động cập nhật Database
        var existingLgIds = await _context.Machines.Select(m => m.LgDeviceId).ToHashSetAsync();
        var newMachines = statusList
            .Where(s => !existingLgIds.Contains(s.DeviceId))
            .Select(s => new QLS.Backend.Models.Machine {
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

    /// <summary>
    /// Lấy chi tiết máy với cấu hình (MachineSetting) + bảng giá hiện tại của cửa hàng
    /// </summary>
    public async Task<MachineDetailWithConfigDto?> GetMachineDetailWithConfigAsync(Guid machineId)
    {
        // 1. Lấy thông tin máy cơ bản
        var machine = await _context.Machines
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == machineId);

        if (machine == null)
            return null;

        // 2. Lấy cấu hình máy (MachineSetting)
        var setting = await _context.MachineSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.MachineId == machineId);

        var settingDto = setting is null
            ? null
            : new MachineSettingDto
            {
                Id = setting.Id,
                MachineId = setting.MachineId,
                Coin = setting.Coin,
                Price = setting.Price ?? [],
                WashingTime = setting.WashingTime,
                WaterLevel = setting.WaterLevel,
                RinsingTime = setting.RinsingTime,
                RinsingCount = setting.RinsingCount,
                SpinSpeed = setting.SpinSpeed,
                TwinSpray = setting.TwinSpray,
                DropCount = setting.DropCount,
                NonStopRinsing = setting.NonStopRinsing,
                AddSuperWash = setting.AddSuperWash,
                DryCycleTime = setting.DryCycleTime,
                TopOffTime = setting.TopOffTime,
                TopOff = setting.TopOff,
                SensingDry = setting.SensingDry,
                TopOffPrice = setting.TopOffPrice,
                RatingMoney = setting.RatingMoney
            };

        // 3. Lấy cửa hàng để xác định brand và loại cửa hàng
        var store = await _context.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == machine.StoreId);

        if (store == null)
            return null;

        // 4. Lấy bảng giá hiện tại (Active) của brand
        PriceListDetailDto? currentPriceList = null;
        try
        {
            var priceListsTask = await _pricingService.GetPriceListsAsync(
                status: PriceListStatus.Active,
                validFrom: DateOnly.FromDateTime(DateTime.UtcNow),
                brandId: store.BrandId);

            // Lấy price list đầu tiên (ưu tiên theo priority hoặc mới nhất)
            var priceListId = priceListsTask?.FirstOrDefault()?.Id;
            if (priceListId.HasValue)
            {
                currentPriceList = await _pricingService.GetPriceListDetailAsync(
                    priceListId.Value,
                    brandId: store.BrandId);
            }
        }
        catch (Exception ex)
        {
            // Log error nhưng không throw - price list là optional
            Console.WriteLine($"Error loading price list: {ex.Message}");
        }

        // 5. Lấy trạng thái LG nếu có LgDeviceId
        string? currentStatus = null;
        bool isOnline = false;
        string? remainTime = null;
        string? currentCourse = null;

        if (!string.IsNullOrEmpty(machine.LgDeviceId) && store.StoreId != null)
        {
            try
            {
                var cred = await _brandLgService.GetValidCredentialAsync(store.BrandId);
                if (cred != null && !string.IsNullOrEmpty(cred.LgUserNo) && !string.IsNullOrEmpty(cred.AccessToken))
                {
                    var rawJson = await _lgClient.GetRawStatusAsync(
                        store.StoreId,
                        cred.LgUserNo,
                        cred.AccessToken);

                    var statusList = LgMapper.MapToDto(rawJson);
                    var machineStatus = statusList.FirstOrDefault(s => s.DeviceId == machine.LgDeviceId);

                    if (machineStatus != null)
                    {
                        currentStatus = machineStatus.CurState;
                        isOnline = machineStatus.Online;
                        currentCourse = machineStatus.Course;
                        if (machineStatus.RemainHour.HasValue || machineStatus.RemainMin.HasValue)
                        {
                            var hours = machineStatus.RemainHour ?? 0;
                            var mins = machineStatus.RemainMin ?? 0;
                            remainTime = $"{hours:D2}:{mins:D2}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error nhưng không throw - trạng thái LG là optional
                Console.WriteLine($"Error loading LG status: {ex.Message}");
            }
        }

        // 6. Xây dựng response DTO
        return new MachineDetailWithConfigDto
        {
            Id = machine.Id,
            StoreId = machine.StoreId,
            Name = machine.Name,
            LgDeviceId = machine.LgDeviceId,
            Esp32MacAddress = machine.Esp32MacAddress,
            ZigbeeNetworkId = machine.ZigbeeNetworkId,
            Type = machine.Type,
            Capacity = machine.Capacity,
            Setting = settingDto,
            CurrentPriceList = currentPriceList,
            CurrentStatus = currentStatus,
            IsOnline = isOnline,
            RemainTime = remainTime,
            CurrentCourse = currentCourse
        };
    }
}