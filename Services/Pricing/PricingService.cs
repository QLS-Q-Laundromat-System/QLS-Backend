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
    public async Task<IEnumerable<TimeSlotDto>> GetAllTimeSlotsAsync(Guid? brandId = null)
    {
        var query = _context.TimeSlots.AsQueryable();

        if (brandId.HasValue)
            query = query.Where(ts => ts.BrandId == brandId.Value);

        return await query
            .Select(ts => new TimeSlotDto
            {
                Id = ts.Id,
                BrandId = ts.BrandId,
                Name = ts.Name,
                Description = ts.Description,
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
            BrandId = dto.BrandId,
            Name = dto.Name,
            Description = dto.Description,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            DayMask = dto.DayMask
        };

        _context.TimeSlots.Add(timeSlot);
        await _context.SaveChangesAsync();

        return new TimeSlotDto
        {
            Id = timeSlot.Id,
            BrandId = timeSlot.BrandId,
            Name = timeSlot.Name,
            Description = timeSlot.Description,
            StartTime = timeSlot.StartTime,
            EndTime = timeSlot.EndTime,
            DayMask = timeSlot.DayMask,
            IsActive = timeSlot.IsActive
        };
    }

    public async Task<TimeSlotDto?> UpdateTimeSlotAsync(Guid id, CreateTimeSlotDto dto, Guid? brandId = null)
    {
        var query = _context.TimeSlots.Where(ts => ts.Id == id);
        if (brandId.HasValue)
            query = query.Where(ts => ts.BrandId == brandId.Value);

        var timeSlot = await query.FirstOrDefaultAsync();
        if (timeSlot == null) return null;

        timeSlot.BrandId = dto.BrandId;
        timeSlot.Name = dto.Name;
        timeSlot.Description = dto.Description;
        timeSlot.StartTime = dto.StartTime;
        timeSlot.EndTime = dto.EndTime;
        timeSlot.DayMask = dto.DayMask;

        await _context.SaveChangesAsync();
        return new TimeSlotDto
        {
            Id = timeSlot.Id,
            BrandId = timeSlot.BrandId,
            Name = timeSlot.Name,
            Description = timeSlot.Description,
            StartTime = timeSlot.StartTime,
            EndTime = timeSlot.EndTime,
            DayMask = timeSlot.DayMask,
            IsActive = timeSlot.IsActive
        };
    }

    public async Task<bool> DeleteTimeSlotAsync(Guid id, Guid? brandId = null)
    {
        var query = _context.TimeSlots.Where(ts => ts.Id == id);
        if (brandId.HasValue)
            query = query.Where(ts => ts.BrandId == brandId.Value);

        var timeSlot = await query.FirstOrDefaultAsync();
        if (timeSlot == null) return false;

        // Xóa cứng hoặc mềm tùy ý, ở đây tôi xóa cứng hoặc set IsActive = false
        // Theo yêu cầu "Xóa/Vô hiệu hóa", tôi sẽ set IsActive = false
        timeSlot.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    // --- PriceLists ---
    public async Task<IEnumerable<PriceListDto>> GetPriceListsAsync(PriceListStatus? status, DateOnly? validFrom, Guid? brandId = null)
    {
        var query = _context.PriceLists.Where(p => !p.IsDeleted);

        if (brandId.HasValue)
            query = query.Where(p => p.BrandId == brandId.Value);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);
        
        if (validFrom.HasValue)
            query = query.Where(p => p.ValidFrom >= validFrom.Value);

        return await query
            .Select(p => new PriceListDto
            {
                Id = p.Id,
                BrandId = p.BrandId,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                ValidFrom = p.ValidFrom,
                ValidTo = p.ValidTo,
                Priority = p.Priority,
                Status = p.Status,
                Currency = p.Currency,
                TaxPercentage = p.TaxPercentage,
                PromotionLabel = p.PromotionLabel
            })
            .ToListAsync();
    }

    public async Task<PriceListDetailDto?> GetPriceListDetailAsync(Guid id, Guid? brandId = null)
    {
        var query = _context.PriceLists
            .Include(p => p.PriceListStoreTypes)
                .ThenInclude(pst => pst.StoreType)
            .Include(p => p.PriceModePerKgs)
            .Include(p => p.PriceModePerSessions)
                .ThenInclude(ps => ps.TimeSlot)
            .Where(p => p.Id == id && !p.IsDeleted);

        if (brandId.HasValue)
            query = query.Where(p => p.BrandId == brandId.Value);

        var p = await query.FirstOrDefaultAsync();

        if (p == null) return null;

        return new PriceListDetailDto
        {
            Id = p.Id,
            BrandId = p.BrandId,
            Code = p.Code,
            Name = p.Name,
            Description = p.Description,
            ValidFrom = p.ValidFrom,
            ValidTo = p.ValidTo,
            Priority = p.Priority,
            Status = p.Status,
            Currency = p.Currency,
            TaxPercentage = p.TaxPercentage,
            PromotionLabel = p.PromotionLabel,
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
                MinimumPrice = m.MinimumPrice,
                PricePer = m.PricePer,
                SortOrder = m.SortOrder
            }).ToList(),
            ModePerSession = p.PriceModePerSessions.Select(m => new PriceModePerSessionItemDto
            {
                MachineType = m.MachineType,
                MachineCapacityKg = m.MachineCapacityKg,
                Price = m.Price,
                DurationMinutes = m.DurationMinutes,
                CycleName = m.CycleName,
                TimeSlotId = m.TimeSlotId,
                TimeSlotName = m.TimeSlot?.Name
            }).ToList()
        };
    }

    public async Task<PriceListDto> CreatePriceListAsync(CreatePriceListDto dto)
    {
        var priceList = new PriceList
        {
            BrandId = dto.BrandId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            Priority = dto.Priority,
            Currency = dto.Currency,
            TaxPercentage = dto.TaxPercentage,
            PromotionLabel = dto.PromotionLabel,
            Status = PriceListStatus.Draft
        };

        _context.PriceLists.Add(priceList);
        await _context.SaveChangesAsync();

        return new PriceListDto
        {
            Id = priceList.Id,
            BrandId = priceList.BrandId,
            Code = priceList.Code,
            Name = priceList.Name,
            Description = priceList.Description,
            ValidFrom = priceList.ValidFrom,
            ValidTo = priceList.ValidTo,
            Priority = priceList.Priority,
            Status = priceList.Status,
            Currency = priceList.Currency,
            TaxPercentage = priceList.TaxPercentage,
            PromotionLabel = priceList.PromotionLabel
        };
    }

    public async Task<bool> UpdatePriceListStatusAsync(Guid id, PriceListStatus status, Guid? brandId = null)
    {
        var query = _context.PriceLists.Where(p => p.Id == id && !p.IsDeleted);
        if (brandId.HasValue)
            query = query.Where(p => p.BrandId == brandId.Value);

        var priceList = await query.FirstOrDefaultAsync();
        if (priceList == null) return false;

        priceList.Status = status;
        priceList.UpdatedAt = DateTimeOffset.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignStoreTypesAsync(Guid id, AssignStoreTypeDto dto, Guid? brandId = null)
    {
        var query = _context.PriceLists
            .Include(p => p.PriceListStoreTypes)
            .Where(p => p.Id == id && !p.IsDeleted);
        
        if (brandId.HasValue)
            query = query.Where(p => p.BrandId == brandId.Value);

        var priceList = await query.FirstOrDefaultAsync();
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

    public async Task<bool> SyncPriceModePerKgAsync(Guid id, List<PriceModePerKgItemDto> modes, Guid? brandId = null)
    {
        var query = _context.PriceLists
            .Include(p => p.PriceModePerKgs)
            .Where(p => p.Id == id && !p.IsDeleted);
            
        if (brandId.HasValue)
            query = query.Where(p => p.BrandId == brandId.Value);

        var priceList = await query.FirstOrDefaultAsync();
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
                MinimumPrice = m.MinimumPrice,
                PricePer = m.PricePer,
                SortOrder = m.SortOrder
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SyncPriceModePerSessionAsync(Guid id, List<PriceModePerSessionItemDto> modes, Guid? brandId = null)
    {
        var query = _context.PriceLists
            .Include(p => p.PriceModePerSessions)
            .Where(p => p.Id == id && !p.IsDeleted);
            
        if (brandId.HasValue)
            query = query.Where(p => p.BrandId == brandId.Value);

        var priceList = await query.FirstOrDefaultAsync();
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
                CycleName = m.CycleName,
                TimeSlotId = m.TimeSlotId
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }
}
