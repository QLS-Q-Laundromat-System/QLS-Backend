using QLS.Backend.DTOs.Pricing;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Interfaces.Pricing;

public interface IPricingService
{
    // TimeSlots
    Task<IEnumerable<TimeSlotDto>> GetAllTimeSlotsAsync(Guid? brandId = null);
    Task<TimeSlotDto> CreateTimeSlotAsync(CreateTimeSlotDto dto);
    Task<TimeSlotDto?> UpdateTimeSlotAsync(Guid id, CreateTimeSlotDto dto, Guid? brandId = null);
    Task<bool> DeleteTimeSlotAsync(Guid id, Guid? brandId = null);

    Task<IEnumerable<PriceListDto>> GetPriceListsAsync(PriceListStatus? status, DateOnly? validFrom, Guid? brandId = null);
    Task<PriceListDetailDto?> GetPriceListDetailAsync(Guid id, Guid? brandId = null);
    Task<PriceListDto> CreatePriceListAsync(CreatePriceListDto dto);
    Task<bool> UpdatePriceListStatusAsync(Guid id, PriceListStatus status, Guid? brandId = null);
    Task<bool> AssignStoreTypesAsync(Guid id, AssignStoreTypeDto dto, Guid? brandId = null);
    Task<bool> SyncPriceModePerKgAsync(Guid id, List<PriceModePerKgItemDto> modes, Guid? brandId = null);
    Task<bool> SyncPriceModePerSessionAsync(Guid id, List<PriceModePerSessionItemDto> modes, Guid? brandId = null);
}
