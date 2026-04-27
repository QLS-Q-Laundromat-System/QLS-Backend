using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.DTOs.Machine;
using QLS.Backend.Interfaces;
using QLS.Backend.Models;

namespace QLS.Backend.Services.MachineSettings;

public class MachineSettingService : IMachineSettingService
{
    private readonly AppDbContext _context;

    public MachineSettingService(AppDbContext context)
    {
        _context = context;
    }

    // ─── GET ──────────────────────────────────────────────────────

    public async Task<MachineSettingDto?> GetByMachineIdAsync(Guid machineId)
    {
        var entity = await _context.MachineSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.MachineId == machineId);

        return entity is null ? null : MapToDto(entity);
    }

    // ─── UPSERT (CREATE hoặc REPLACE hoàn toàn) ──────────────────

    public async Task<MachineSettingDto> UpsertAsync(Guid machineId, UpsertMachineSettingDto dto)
    {
        // Kiểm tra máy tồn tại
        var machineExists = await _context.Machines.AnyAsync(m => m.Id == machineId);
        if (!machineExists)
            throw new KeyNotFoundException($"Không tìm thấy máy với Id = {machineId}.");

        var existing = await _context.MachineSettings
            .FirstOrDefaultAsync(s => s.MachineId == machineId);

        if (existing is null)
        {
            // CREATE
            var newSetting = new MachineSetting
            {
                MachineId = machineId,
            };
            ApplyDto(newSetting, dto);
            _context.MachineSettings.Add(newSetting);
            await _context.SaveChangesAsync();
            return MapToDto(newSetting);
        }
        else
        {
            // REPLACE hoàn toàn
            ApplyDto(existing, dto);
            _context.MachineSettings.Update(existing);
            await _context.SaveChangesAsync();
            return MapToDto(existing);
        }
    }

    // ─── DELETE ───────────────────────────────────────────────────

    public async Task<bool> DeleteAsync(Guid machineId)
    {
        var entity = await _context.MachineSettings
            .FirstOrDefaultAsync(s => s.MachineId == machineId);

        if (entity is null) return false;

        _context.MachineSettings.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    // ─── HELPERS ──────────────────────────────────────────────────

    private static void ApplyDto(MachineSetting entity, UpsertMachineSettingDto dto)
    {
        entity.Coin = dto.Coin;
        entity.Price = dto.Price;

        entity.WashingTime = dto.WashingTime;
        entity.WaterLevel = dto.WaterLevel;
        entity.RinsingTime = dto.RinsingTime;
        entity.RinsingCount = dto.RinsingCount;
        entity.SpinSpeed = dto.SpinSpeed;
        entity.TwinSpray = dto.TwinSpray;
        entity.DropCount = dto.DropCount;
        entity.NonStopRinsing = dto.NonStopRinsing;
        entity.AddSuperWash = dto.AddSuperWash;

        entity.DryCycleTime = dto.DryCycleTime;
        entity.TopOffTime = dto.TopOffTime;
        entity.TopOff = dto.TopOff;
        entity.SensingDry = dto.SensingDry;
        entity.TopOffPrice = dto.TopOffPrice;
        entity.RatingMoney = dto.RatingMoney;
    }

    private static MachineSettingDto MapToDto(MachineSetting entity) => new()
    {
        Id = entity.Id,
        MachineId = entity.MachineId,

        Coin = entity.Coin,
        Price = entity.Price,
        
        WashingTime = entity.WashingTime,
        WaterLevel = entity.WaterLevel,
        RinsingTime = entity.RinsingTime,
        RinsingCount = entity.RinsingCount,
        SpinSpeed = entity.SpinSpeed,
        TwinSpray = entity.TwinSpray,
        DropCount = entity.DropCount,
        NonStopRinsing = entity.NonStopRinsing,
        AddSuperWash = entity.AddSuperWash,

        DryCycleTime = entity.DryCycleTime,
        TopOffTime = entity.TopOffTime,
        TopOff = entity.TopOff,
        SensingDry = entity.SensingDry,
        TopOffPrice = entity.TopOffPrice,
        RatingMoney = entity.RatingMoney,
    };
}
