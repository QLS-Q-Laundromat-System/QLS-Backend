using QLS.Backend.DTOs.Pricing;

namespace QLS.Backend.Interfaces.Pricing;

public interface IPricingCalculatorService
{
    Task<PriceCalculationResponseDto?> CalculatePriceAsync(CalculatePriceRequestDto dto);
}
