using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.DTOs.Machine;
using QLS.Backend.Interfaces;
using QLS.Backend.Interfaces.Brand;
using QLS.Backend.Services.LgService;

namespace QLS.Backend.Services.MachineSettings;

/// <summary>
/// Service lấy setting của một machine:
///  - Nếu DB đã có setting → trả về ngay.
///  - Nếu chưa có → gọi LG API, parse, lưu vào DB rồi trả về.
/// </summary>
public class LgMachineSettingSyncService : ILgMachineSettingSyncService
{
    private readonly AppDbContext _context;
    private readonly LgApiClient _lgClient;
    private readonly IBrandLgService _brandLgService;
    private readonly IMachineSettingService _settingService;

    public LgMachineSettingSyncService(
        AppDbContext context,
        LgApiClient lgClient,
        IBrandLgService brandLgService,
        IMachineSettingService settingService)
    {
        _context = context;
        _lgClient = lgClient;
        _brandLgService = brandLgService;
        _settingService = settingService;
    }

    public async Task<MachineSettingDto> GetOrFetchSettingAsync(Guid machineId)
    {
        // 1. Kiểm tra DB đã có setting chưa
        var existingSetting = await _settingService.GetByMachineIdAsync(machineId);
        if (existingSetting is not null)
            return existingSetting;

        // 2. Chưa có → cần lấy machine để biết LgDeviceId và Store
        var machine = await _context.Machines
            .AsNoTracking()
            .Include(m => m.Setting)
            .FirstOrDefaultAsync(m => m.Id == machineId)
            ?? throw new KeyNotFoundException($"Không tìm thấy máy với Id = {machineId}.");

        if (string.IsNullOrEmpty(machine.LgDeviceId))
            throw new InvalidOperationException($"Máy '{machine.Name}' chưa được liên kết với thiết bị LG (LgDeviceId trống).");

        // 3. Lấy LG credential qua Store → Brand
        var store = await _context.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == machine.StoreId)
            ?? throw new InvalidOperationException("Không tìm thấy cửa hàng liên kết với máy này.");

        var cred = await _brandLgService.GetValidCredentialAsync(store.BrandId)
            ?? throw new InvalidOperationException("Thương hiệu chưa được cấu hình tài khoản kết nối LG.");

        // 4. Gọi LG API lấy setting
        var rawJson = await _lgClient.GetRawSettingsAsync(machine.LgDeviceId, cred.LgUserNo, cred.AccessToken);

        // 5. Parse JSON → UpsertMachineSettingDto (phân biệt Washer / Dryer theo machine.Type)
        var dto = ParseLgSettingJson(rawJson, machine.Type);

        // 6. Upsert vào DB rồi trả về
        return await _settingService.UpsertAsync(machineId, dto);
    }

    // ────────────────────────────────────────────────────────────────
    // PARSE JSON từ LG API /devices/{deviceId}/settings
    // Washer (Type == Washer):
    //   { washingTime, waterLevel, rinsingTime, rinsingCount, dropCount,
    //     twinSpray, price[5], coin, addSuperWash, nonStopRinsing, spinSpeed }
    // Dryer (Type == Dryer):
    //   { dryCycleTime, topOffTime, topOff, sensingDry, regularPrice[5], topOffPrice[4], coin1 }
    // ────────────────────────────────────────────────────────────────
    private static UpsertMachineSettingDto ParseLgSettingJson(string rawJson, Models.Enums.MachineType machineType)
    {
        using var doc = JsonDocument.Parse(rawJson);

        if (!doc.RootElement.TryGetProperty("result", out var result))
            throw new InvalidOperationException("LG API không trả về 'result' trong response setting.");

        var dto = new UpsertMachineSettingDto();

        if (machineType == Models.Enums.MachineType.Washer)
        {
            // ── Máy Giặt ────────────────────────────────────────────
            dto.WashingTime  = ReadIntArray(result, "washingTime");
            dto.WaterLevel   = ReadIntArray(result, "waterLevel");
            dto.RinsingTime  = ReadIntArray(result, "rinsingTime");
            dto.RinsingCount = ReadIntArray(result, "rinsingCount");
            dto.SpinSpeed    = ReadStringArray(result, "spinSpeed");
            dto.TwinSpray    = ReadString(result, "twinSpray");
            dto.DropCount    = ReadNullableInt(result, "dropCount");
            dto.NonStopRinsing = ReadNullableBool(result, "nonStopRinsing");
            dto.Price        = ReadIntArray(result, "price") ?? [];
            dto.Coin         = ReadInt(result, "coin");
            dto.AddSuperWash = ReadInt(result, "addSuperWash");
        }
        else
        {
            // ── Máy Sấy ─────────────────────────────────────────────
            dto.DryCycleTime = ReadIntArray(result, "dryCycleTime");
            dto.TopOffTime   = ReadIntArray(result, "topOffTime");
            dto.TopOff       = ReadString(result, "topOff");
            dto.SensingDry   = ReadString(result, "sensingDry");
            dto.TopOffPrice  = ReadIntArray(result, "topOffPrice");
            dto.Price        = ReadIntArray(result, "regularPrice") ?? [];
            dto.Coin         = ReadInt(result, "coin1");
            dto.AddSuperWash = ReadInt(result, "addSuperWash");
        }

        return dto;
    }

    // ─── HELPERS ──────────────────────────────────────────────────
    private static int[] ReadIntArray(JsonElement el, string key)
    {
        if (!el.TryGetProperty(key, out var prop) || prop.ValueKind != JsonValueKind.Array)
            return [];
        return prop.EnumerateArray()
                   .Select(x => x.ValueKind == JsonValueKind.Number ? x.GetInt32() : 0)
                   .ToArray();
    }

    private static string[] ReadStringArray(JsonElement el, string key)
    {
        if (!el.TryGetProperty(key, out var prop) || prop.ValueKind != JsonValueKind.Array)
            return [];
        return prop.EnumerateArray()
                   .Select(x => x.GetString() ?? "")
                   .ToArray();
    }

    private static string? ReadString(JsonElement el, string key)
    {
        if (!el.TryGetProperty(key, out var prop)) return null;
        return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.GetRawText();
    }

    private static int ReadInt(JsonElement el, string key)
    {
        if (!el.TryGetProperty(key, out var prop) || prop.ValueKind != JsonValueKind.Number)
            return 0;
        return prop.GetInt32();
    }

    private static int? ReadNullableInt(JsonElement el, string key)
    {
        if (!el.TryGetProperty(key, out var prop) || prop.ValueKind != JsonValueKind.Number)
            return null;
        return prop.GetInt32();
    }

    private static bool? ReadNullableBool(JsonElement el, string key)
    {
        if (!el.TryGetProperty(key, out var prop)) return null;
        if (prop.ValueKind == JsonValueKind.True) return true;
        if (prop.ValueKind == JsonValueKind.False) return false;
        return null;
    }
}
