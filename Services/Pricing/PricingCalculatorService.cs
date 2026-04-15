using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.DTOs.Pricing;
using QLS.Backend.Interfaces.Pricing;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Services.Pricing;

public class PricingCalculatorService : IPricingCalculatorService
{
    private readonly AppDbContext _context;

    public PricingCalculatorService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PriceCalculationResponseDto?> CalculatePriceAsync(CalculatePriceRequestDto dto)
    {
        var calculateTime = dto.CalculateTime ?? DateTime.Now;
        var currentTime = TimeOnly.FromDateTime(calculateTime);
        var currentDate = DateOnly.FromDateTime(calculateTime);
        var currentDay = calculateTime.DayOfWeek;

        // 1. Lấy thông tin Store và StoreType
        var store = await _context.Stores
            .Include(s => s.StoreType)
            .FirstOrDefaultAsync(s => s.Id == dto.StoreId);

        if (store == null || store.StoreTypeId == null) return null;

        // 2 & 3. Tìm Bảng giá (PriceList) đang có hiệu lực và có ưu tiên cao nhất
        // Thêm điều kiện lọc theo BrandId của cửa hàng và kiểm tra IsDeleted
        var priceList = await _context.PriceLists
            .Where(p => p.BrandId == store.BrandId && 
                        !p.IsDeleted &&
                        p.Status == PriceListStatus.Active &&
                        p.ValidFrom <= currentDate &&
                        (p.ValidTo == null || p.ValidTo >= currentDate))
            .Join(_context.PriceListStoreTypes,
                p => p.Id,
                pst => pst.PriceListId,
                (p, pst) => new { PriceList = p, StoreTypeRelation = pst })
            .Where(x => x.StoreTypeRelation.StoreTypeId == store.StoreTypeId)
            .OrderByDescending(x => x.StoreTypeRelation.OverridePriority ?? x.PriceList.Priority)
            .ThenByDescending(x => x.PriceList.CreatedAt)
            .Select(x => x.PriceList)
            .FirstOrDefaultAsync();

        if (priceList == null) return null;

        // 4. Tính toán giá
        // Ưu tiên Mode 1 (PerKg) nếu có số cân nặng, ngược lại dùng Mode 2 (PerSession)
        if (dto.ClothingWeightKg.HasValue && dto.ClothingWeightKg.Value > 0)
        {
            var modeKg = await _context.PriceModePerKgs
                .Where(m => m.PriceListId == priceList.Id && m.MachineType == dto.MachineType)
                .Where(m => m.MinKg <= dto.ClothingWeightKg.Value && 
                           (m.MaxKg == null || m.MaxKg >= dto.ClothingWeightKg.Value))
                .OrderBy(m => m.SortOrder)
                .FirstOrDefaultAsync();

            if (modeKg != null)
            {
                decimal calculatedPrice = 0;
                if (modeKg.PricePer == PricePerType.PerKg)
                {
                    calculatedPrice = modeKg.UnitPrice * dto.ClothingWeightKg.Value;
                }
                else // Flat
                {
                    calculatedPrice = modeKg.UnitPrice;
                }

                // Áp dụng Giá tối thiểu (MinimumPrice)
                decimal finalPrice = Math.Max(calculatedPrice, modeKg.MinimumPrice);

                return new PriceCalculationResponseDto
                {
                    FinalPrice = finalPrice,
                    PriceListId = priceList.Id,
                    PriceListName = priceList.Name,
                    Mode = "PerKg",
                    CalculationDetail = $"Áp dụng mức {modeKg.MinKg}-{modeKg.MaxKg ?? 99} kg. " +
                                       (finalPrice > calculatedPrice ? $"Giá tối thiểu áp dụng: {finalPrice:N0}. " : "") +
                                       $"Cách tính: {(modeKg.PricePer == PricePerType.PerKg ? $"{dto.ClothingWeightKg}kg * {modeKg.UnitPrice:N0}" : "Trọn gói")}"
                };
            }
        }

        // Mode 2 (PerSession)
        var modeSessions = await _context.PriceModePerSessions
            .Include(m => m.TimeSlot)
            .Where(m => m.PriceListId == priceList.Id && 
                        m.MachineType == dto.MachineType &&
                        m.MachineCapacityKg == dto.MachineCapacityKg)
            .ToListAsync();

        // Lọc theo khung giờ (TimeSlot)
        var matchedMode = modeSessions
            .Where(m => 
            {
                if (m.TimeSlot == null) return true; // Không có slot -> áp dụng mọi lúc
                
                // Kiểm tra ngày trong tuần (Bitmask)
                if (!IsDayMatch(currentDay, m.TimeSlot.DayMask)) return false;

                // Kiểm tra giờ
                if (m.TimeSlot.StartTime.HasValue && currentTime < m.TimeSlot.StartTime.Value) return false;
                if (m.TimeSlot.EndTime.HasValue && currentTime > m.TimeSlot.EndTime.Value) return false;

                return true;
            })
            .OrderByDescending(m => m.TimeSlotId.HasValue) // Ưu tiên cái có khung giờ cụ thể hơn là cái mặc định
            .FirstOrDefault();

        if (matchedMode != null)
        {
            return new PriceCalculationResponseDto
            {
                FinalPrice = matchedMode.Price,
                PriceListId = priceList.Id,
                PriceListName = priceList.Name,
                Mode = "PerSession",
                CalculationDetail = $"Máy {dto.MachineCapacityKg}kg, thời lượng {matchedMode.DurationMinutes} phút. " +
                                   (!string.IsNullOrEmpty(matchedMode.CycleName) ? $"Dịch vụ: {matchedMode.CycleName}. " : "") +
                                   (matchedMode.TimeSlot != null ? $"Khung giờ: {matchedMode.TimeSlot.Name}" : "Giá mặc định")
            };
        }

        return null;
    }

    private bool IsDayMatch(DayOfWeek day, DayOfWeekMask mask)
    {
        DayOfWeekMask currentDayMask = day switch
        {
            DayOfWeek.Monday => DayOfWeekMask.Monday,
            DayOfWeek.Tuesday => DayOfWeekMask.Tuesday,
            DayOfWeek.Wednesday => DayOfWeekMask.Wednesday,
            DayOfWeek.Thursday => DayOfWeekMask.Thursday,
            DayOfWeek.Friday => DayOfWeekMask.Friday,
            DayOfWeek.Saturday => DayOfWeekMask.Saturday,
            DayOfWeek.Sunday => DayOfWeekMask.Sunday,
            _ => DayOfWeekMask.None
        };
        return (mask & currentDayMask) != 0;
    }
}
