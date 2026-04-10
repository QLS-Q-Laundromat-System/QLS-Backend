using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.DTOs.Pricing;
using QLS.Backend.Interfaces.Pricing;
using QLS.Backend.Models;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Services.Pricing;

public class PricingService : IPricingService
{
    private readonly AppDbContext _context;

    public PricingService(AppDbContext context)
    {
        _context = context;
    }

    // --- TimeSlots ---
    public async Task<IEnumerable<TimeSlotDto>> GetAllTimeSlotsAsync()
    {
        return await _context.TimeSlots
            .Select(ts => new TimeSlotDto
            {
                Id = ts.Id,
                Name = ts.Name,
                StartTime = ts.StartTime,
                EndTime = ts.EndTime,
                DayMask = ts.DayMask,
                IsActive = ts.IsActive
            })
            .ToListAsync();
    }

    public async Task<TimeSlotDto> CreateTimeSlotAsync(CreateTimeSlotDto dto)
    {
        var timeSlot = new TimeSlot
        {
            Name = dto.Name,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            DayMask = dto.DayMask
        };

        _context.TimeSlots.Add(timeSlot);
        await _context.SaveChangesAsync();

        return new TimeSlotDto
        {
            Id = timeSlot.Id,
            Name = timeSlot.Name,
            StartTime = timeSlot.StartTime,
            EndTime = timeSlot.EndTime,
            DayMask = timeSlot.DayMask,
            IsActive = timeSlot.IsActive
        };
    }

    public async Task<TimeSlotDto?> UpdateTimeSlotAsync(Guid id, CreateTimeSlotDto dto)
    {
        var timeSlot = await _context.TimeSlots.FindAsync(id);
        if (timeSlot == null) return null;

        timeSlot.Name = dto.Name;
        timeSlot.StartTime = dto.StartTime;
        timeSlot.EndTime = dto.EndTime;
        timeSlot.DayMask = dto.DayMask;

        await _context.SaveChangesAsync();
        return new TimeSlotDto
        {
            Id = timeSlot.Id,
            Name = timeSlot.Name,
            StartTime = timeSlot.StartTime,
            EndTime = timeSlot.EndTime,
            DayMask = timeSlot.DayMask,
            IsActive = timeSlot.IsActive
        };
    }

    public async Task<bool> DeleteTimeSlotAsync(Guid id)
    {
        var timeSlot = await _context.TimeSlots.FindAsync(id);
        if (timeSlot == null) return false;

        // Xóa cứng hoặc mềm tùy ý, ở đây tôi xóa cứng hoặc set IsActive = false
        // Theo yêu cầu "Xóa/Vô hiệu hóa", tôi sẽ set IsActive = false
        timeSlot.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    // --- PriceLists ---
    public async Task<IEnumerable<PriceListDto>> GetPriceListsAsync(PriceListStatus? status, DateOnly? validFrom)
    {
        var query = _context.PriceLists.AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);
        
        if (validFrom.HasValue)
            query = query.Where(p => p.ValidFrom >= validFrom.Value);

        return await query
            .Select(p => new PriceListDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                ValidFrom = p.ValidFrom,
                ValidTo = p.ValidTo,
                Priority = p.Priority,
                Status = p.Status,
                Currency = p.Currency
            })
            .ToListAsync();
    }

    public async Task<PriceListDetailDto?> GetPriceListDetailAsync(Guid id)
    {
        var p = await _context.PriceLists
            .Include(p => p.PriceListStoreTypes)
                .ThenInclude(pst => pst.StoreType)
            .Include(p => p.PriceModePerKgs)
            .Include(p => p.PriceModePerSessions)
                .ThenInclude(ps => ps.TimeSlot)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (p == null) return null;

        return new PriceListDetailDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Description = p.Description,
            ValidFrom = p.ValidFrom,
            ValidTo = p.ValidTo,
            Priority = p.Priority,
            Status = p.Status,
            Currency = p.Currency,
            StoreTypes = p.PriceListStoreTypes.Select(pst => new PriceListStoreTypeItemDto
            {
                StoreTypeId = pst.StoreTypeId,
                StoreTypeName = pst.StoreType?.Name,
                OverridePriority = pst.OverridePriority
            }).ToList(),
            ModePerKg = p.PriceModePerKgs.Select(m => new PriceModePerKgItemDto
            {
                MachineType = m.MachineType,
                MinKg = m.MinKg,
                MaxKg = m.MaxKg,
                UnitPrice = m.UnitPrice,
                PricePer = m.PricePer,
                SortOrder = m.SortOrder
            }).ToList(),
            ModePerSession = p.PriceModePerSessions.Select(m => new PriceModePerSessionItemDto
            {
                MachineType = m.MachineType,
                MachineCapacityKg = m.MachineCapacityKg,
                Price = m.Price,
                DurationMinutes = m.DurationMinutes,
                TimeSlotId = m.TimeSlotId,
                TimeSlotName = m.TimeSlot?.Name
            }).ToList()
        };
    }

    public async Task<PriceListDto> CreatePriceListAsync(CreatePriceListDto dto)
    {
        var priceList = new PriceList
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            Priority = dto.Priority,
            Currency = dto.Currency,
            Status = PriceListStatus.Draft
        };

        _context.PriceLists.Add(priceList);
        await _context.SaveChangesAsync();

        return new PriceListDto
        {
            Id = priceList.Id,
            Code = priceList.Code,
            Name = priceList.Name,
            Description = priceList.Description,
            ValidFrom = priceList.ValidFrom,
            ValidTo = priceList.ValidTo,
            Priority = priceList.Priority,
            Status = priceList.Status,
            Currency = priceList.Currency
        };
    }

    public async Task<bool> UpdatePriceListStatusAsync(Guid id, PriceListStatus status)
    {
        var priceList = await _context.PriceLists.FindAsync(id);
        if (priceList == null) return false;

        priceList.Status = status;
        priceList.UpdatedAt = DateTimeOffset.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignStoreTypesAsync(Guid id, AssignStoreTypeDto dto)
    {
        var priceList = await _context.PriceLists
            .Include(p => p.PriceListStoreTypes)
            .FirstOrDefaultAsync(p => p.Id == id);
        
        if (priceList == null) return false;

        // Xóa gán cũ
        _context.PriceListStoreTypes.RemoveRange(priceList.PriceListStoreTypes);

        // Thêm gán mới
        foreach (var stDto in dto.StoreTypes)
        {
            _context.PriceListStoreTypes.Add(new PriceListStoreType
            {
                PriceListId = id,
                StoreTypeId = stDto.StoreTypeId,
                OverridePriority = stDto.OverridePriority
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SyncPriceModePerKgAsync(Guid id, List<PriceModePerKgItemDto> modes)
    {
        var priceList = await _context.PriceLists
            .Include(p => p.PriceModePerKgs)
            .FirstOrDefaultAsync(p => p.Id == id);
            
        if (priceList == null) return false;

        _context.PriceModePerKgs.RemoveRange(priceList.PriceModePerKgs);

        foreach (var m in modes)
        {
            _context.PriceModePerKgs.Add(new PriceModePerKg
            {
                PriceListId = id,
                MachineType = m.MachineType,
                MinKg = m.MinKg,
                MaxKg = m.MaxKg,
                UnitPrice = m.UnitPrice,
                PricePer = m.PricePer,
                SortOrder = m.SortOrder
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SyncPriceModePerSessionAsync(Guid id, List<PriceModePerSessionItemDto> modes)
    {
        var priceList = await _context.PriceLists
            .Include(p => p.PriceModePerSessions)
            .FirstOrDefaultAsync(p => p.Id == id);
            
        if (priceList == null) return false;

        _context.PriceModePerSessions.RemoveRange(priceList.PriceModePerSessions);

        foreach (var m in modes)
        {
            _context.PriceModePerSessions.Add(new PriceModePerSession
            {
                PriceListId = id,
                MachineType = m.MachineType,
                MachineCapacityKg = m.MachineCapacityKg,
                Price = m.Price,
                DurationMinutes = m.DurationMinutes,
                TimeSlotId = m.TimeSlotId
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }
}
