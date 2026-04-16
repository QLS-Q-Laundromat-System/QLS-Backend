using QLS.Backend.Models.Enums;

namespace QLS.Backend.DTOs.Pricing;

public class PriceListDto
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public int Priority { get; set; }
    public PriceListStatus Status { get; set; }
    public Currency Currency { get; set; }
    public decimal TaxPercentage { get; set; }
    public string? PromotionLabel { get; set; }
}

public class CreatePriceListDto
{
    public Guid BrandId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public int Priority { get; set; }
    public Currency Currency { get; set; } = Currency.VND;
    public decimal TaxPercentage { get; set; } = 0;
    public string? PromotionLabel { get; set; }
}

public class PriceListDetailDto : PriceListDto
{
    public List<PriceListStoreTypeItemDto> StoreTypes { get; set; } = [];
    public List<PriceModePerKgItemDto> ModePerKg { get; set; } = [];
    public List<PriceModePerSessionItemDto> ModePerSession { get; set; } = [];
}

public class PriceListStoreTypeItemDto
{
    public Guid StoreTypeId { get; set; }
    public string? StoreTypeName { get; set; }
    public int? OverridePriority { get; set; }
}

public class PriceModePerKgItemDto
{
    public MachineType MachineType { get; set; }
    public decimal MinKg { get; set; }
    public decimal? MaxKg { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal MinimumPrice { get; set; }
    public PricePerType PricePer { get; set; }
    public int SortOrder { get; set; }
}

public class PriceModePerSessionItemDto
{
    public MachineType MachineType { get; set; }
    public decimal MachineCapacityKg { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public Guid? TimeSlotId { get; set; }
    public string? TimeSlotName { get; set; }

    // --- RIÊNG CỦA MÁY GIẶT ---
    public string? CycleName { get; set; }

    // --- RIÊNG CỦA MÁY SẤY ---
    public int? MinInitialSteps { get; set; }
    public int? ExtensionTimeoutMinutes { get; set; }
}

public class UpdatePriceListStatusDto
{
    public PriceListStatus Status { get; set; }
}

public class AssignPriceListStoreTypesDto
{
    public List<PriceListStoreTypeItemDto> StoreTypes { get; set; } = [];
}

public class CalculatePriceRequestDto
{
    public Guid StoreId { get; set; }
    public MachineType MachineType { get; set; }
    public decimal MachineCapacityKg { get; set; }
    public decimal? ClothingWeightKg { get; set; }
    public DateTime? CalculateTime { get; set; }
}

public class PriceCalculationResponseDto
{
    public decimal FinalPrice { get; set; }
    public Guid PriceListId { get; set; }
    public string PriceListName { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public string CalculationDetail { get; set; } = string.Empty;
    
    // --- RIÊNG CỦA MÁY SẤY ---
    public int? MinInitialSteps { get; set; }
    public int? DurationMinutes { get; set; }
    public int? ExtensionTimeoutMinutes { get; set; }
}
