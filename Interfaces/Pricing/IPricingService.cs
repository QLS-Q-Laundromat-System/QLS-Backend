using QLS.Backend.DTOs.Pricing;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Interfaces.Pricing;

public interface IPricingService
{
    // TimeSlots
    Task<IEnumerable<TimeSlotDto>> GetAllTimeSlotsAsync();
    Task<TimeSlotDto> CreateTimeSlotAsync(CreateTimeSlotDto dto);
    Task<TimeSlotDto?> UpdateTimeSlotAsync(Guid id, CreateTimeSlotDto dto);
    Task<bool> DeleteTimeSlotAsync(Guid id);

    // PriceLists
    Task<IEnumerable<PriceListDto>> GetPriceListsAsync(PriceListStatus? status, DateOnly? validFrom);
    Task<PriceListDetailDto?> GetPriceListDetailAsync(Guid id);
    Task<PriceListDto> CreatePriceListAsync(CreatePriceListDto dto);
    Task<bool> UpdatePriceListStatusAsync(Guid id, PriceListStatus status);
    Task<bool> AssignStoreTypesAsync(Guid id, AssignStoreTypeDto dto);
    Task<bool> SyncPriceModePerKgAsync(Guid id, List<PriceModePerKgItemDto> modes);
    Task<bool> SyncPriceModePerSessionAsync(Guid id, List<PriceModePerSessionItemDto> modes);
}
