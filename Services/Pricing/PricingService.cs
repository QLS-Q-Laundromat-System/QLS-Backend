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
            .AsNoTracking()
            .Include(p => p.PriceListStoreTypes)
                .ThenInclude(pst => pst.StoreType)
            .Include(p => p.PriceModePerKgs)
            .Include(p => p.PriceModePerSessions)
                .ThenInclude(ps => ps.TimeSlot)
            .AsSplitQuery()
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
                MachineType = m is WasherPriceMode ? MachineType.Washer : MachineType.Dryer,
                MachineCapacityKg = m.MachineCapacityKg,
                Price = m.Price,
                DurationMinutes = m.DurationMinutes,
                TimeSlotId = m.TimeSlotId,
                TimeSlotName = m.TimeSlot?.Name,
                CycleName = (m as WasherPriceMode)?.CycleName,
                MinInitialSteps = (m as DryerPriceMode)?.MinInitialSteps,
                ExtensionTimeoutMinutes = (m as DryerPriceMode)?.ExtensionTimeoutMinutes
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

    public async Task<bool> AssignStoreTypesAsync(Guid id, AssignPriceListStoreTypesDto dto, Guid? brandId = null)
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

        // Xóa gán cũ
        _context.PriceModePerSessions.RemoveRange(priceList.PriceModePerSessions);

        // Thêm gán mới
        foreach (var m in modes)
        {
            // Khai báo class cha
            PriceModePerSession newMode;

            // Rẽ nhánh dựa vào MachineType Frontend gửi lên để khởi tạo đúng Class con
            if (m.MachineType == MachineType.Washer)
            {
                newMode = new WasherPriceMode
                {
                    PriceListId = id,
                    MachineCapacityKg = m.MachineCapacityKg,
                    Price = m.Price,
                    DurationMinutes = m.DurationMinutes,
                    TimeSlotId = m.TimeSlotId,
                    
                    // Gán trường riêng của máy giặt
                    CycleName = m.CycleName 
                };
            }
            else if (m.MachineType == MachineType.Dryer)
            {
                newMode = new DryerPriceMode
                {
                    PriceListId = id,
                    MachineCapacityKg = m.MachineCapacityKg,
                    Price = m.Price,
                    DurationMinutes = m.DurationMinutes,
                    TimeSlotId = m.TimeSlotId,
                    
                    // Gán trường riêng của máy sấy
                    MinInitialSteps = m.MinInitialSteps,
                    ExtensionTimeoutMinutes = m.ExtensionTimeoutMinutes
                };
            }
            else
            {
                throw new ArgumentException("MachineType không hợp lệ trong quá trình đồng bộ giá.");
            }

            // Entity Framework Core đủ thông minh để nhận ra đây là class con 
            // và tự động lưu đúng "Discriminator" xuống DB
            _context.PriceModePerSessions.Add(newMode);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<PriceCalculationResponseDto> CalculatePriceAsync(CalculatePriceRequestDto dto)
    {
        var calculateTime = dto.CalculateTime ?? DateTime.UtcNow.AddHours(7);

        // 1. Lấy thông tin cửa hàng để biết BrandId và StoreTypeId
        var store = await _context.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == dto.StoreId);

        if (store == null)
            throw new Exception("Không tìm thấy cửa hàng.");

        // 2. Tìm bảng giá phù hợp nhất
        // Ưu tiên: Trạng thái Active > Đúng Brand > Trong hạn sử dụng > Đúng StoreType (nếu có cấu hình) > Priority cao nhất
        var priceListQuery = _context.PriceLists
            .AsNoTracking()
            .Include(p => p.PriceListStoreTypes)
            .Include(p => p.PriceModePerKgs)
            .Include(p => p.PriceModePerSessions)
                .ThenInclude(m => m.TimeSlot)
            .Where(p => p.BrandId == store.BrandId 
                   && p.Status == PriceListStatus.Active 
                   && !p.IsDeleted
                   && p.ValidFrom <= DateOnly.FromDateTime(calculateTime)
                   && (p.ValidTo == null || p.ValidTo >= DateOnly.FromDateTime(calculateTime)));

        var priceLists = await priceListQuery.ToListAsync();

        // Lọc theo StoreType: 
        // - Nếu bảng giá có cấu hình StoreType, cửa hàng phải thuộc StoreType đó.
        // - Nếu bảng giá không cấu hình StoreType nào, coi như áp dụng cho tất cả.
        var applicablePriceList = priceLists
            .Where(p => !p.PriceListStoreTypes.Any() || (store.StoreTypeId.HasValue && p.PriceListStoreTypes.Any(pst => pst.StoreTypeId == store.StoreTypeId.Value)))
            .OrderByDescending(p => p.Priority)
            .ThenByDescending(p => p.CreatedAt)
            .FirstOrDefault();

        if (applicablePriceList == null)
            throw new Exception("Không tìm thấy bảng giá nào áp dụng cho cửa hàng này tại thời điểm hiện tại.");

        var response = new PriceCalculationResponseDto
        {
            PriceListId = applicablePriceList.Id,
            PriceListName = applicablePriceList.Name
        };

        // 3. Tính toán giá dựa trên Mode
        
        // --- TRƯỜNG HỢP 1: TÍNH THEO CÂN NẶNG (Ưu tiên nếu có truyền ClothingWeightKg) ---
        if (dto.ClothingWeightKg.HasValue && dto.ClothingWeightKg.Value > 0)
        {
            var weight = dto.ClothingWeightKg.Value;
            var mode = applicablePriceList.PriceModePerKgs
                .Where(m => m.MachineType == dto.MachineType && weight >= m.MinKg && (m.MaxKg == null || weight <= m.MaxKg))
                .OrderBy(m => m.SortOrder)
                .FirstOrDefault();

            if (mode != null)
            {
                decimal calculatedPrice = mode.PricePer == PricePerType.PerKg 
                    ? mode.UnitPrice * weight 
                    : mode.UnitPrice;

                response.FinalPrice = Math.Max(mode.MinimumPrice, calculatedPrice);
                response.Mode = "PerKg";
                response.CalculationDetail = $"Bảng giá {applicablePriceList.Name}: {weight}kg x {mode.UnitPrice:N0} ({mode.PricePer}). Min: {mode.MinimumPrice:N0}";
                return response;
            }
        }

        // --- TRƯỜNG HỢP 2: TÍNH THEO LƯỢT (SESSION) ---
        var timeOnly = TimeOnly.FromDateTime(calculateTime);
        var dayOfWeek = calculateTime.DayOfWeek;
        // Chuyển DayOfWeek sang Mask (Sunday = 1, Monday = 2, ...)
        var currentDayMask = (DayOfWeekMask)(1 << (int)dayOfWeek);

        var sessionMode = applicablePriceList.PriceModePerSessions
            .Where(m => m.MachineCapacityKg == dto.MachineCapacityKg)
            .Where(m => (dto.MachineType == MachineType.Washer && m is WasherPriceMode) || 
                        (dto.MachineType == MachineType.Dryer && m is DryerPriceMode))
            .Where(m => m.TimeSlot == null || 
                        (m.TimeSlot.IsActive && 
                         (m.TimeSlot.DayMask & currentDayMask) != 0 &&
                         (m.TimeSlot.StartTime == null || timeOnly >= m.TimeSlot.StartTime) &&
                         (m.TimeSlot.EndTime == null || timeOnly <= m.TimeSlot.EndTime)))
            .FirstOrDefault();

        if (sessionMode != null)
        {
            response.FinalPrice = sessionMode.Price;
            response.Mode = "PerSession";
            
            var detail = $"Bảng giá {applicablePriceList.Name}: Máy {dto.MachineCapacityKg}kg - {sessionMode.DurationMinutes} phút - {sessionMode.Price:N0} VNĐ";
            
            if (sessionMode.TimeSlot != null)
            {
                detail += $" [Khung giờ: {sessionMode.TimeSlot.Name}]";
            }

            // Bổ sung thông tin riêng cho máy sấy
            if (sessionMode is DryerPriceMode dMode)
            {
                detail += $" (Sấy tối thiểu: {dMode.MinInitialSteps} bước, Thời gian chờ gia hạn: {dMode.ExtensionTimeoutMinutes} phút)";
            }
            
            response.CalculationDetail = detail;
            return response;
        }

        throw new Exception($"Không tìm thấy cấu hình giá phù hợp cho máy {dto.MachineType} công suất {dto.MachineCapacityKg}kg.");
    }
}
