using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs;
using QLS.Backend.DTOs.Pricing;
using QLS.Backend.Interfaces.Pricing;
using QLS.Backend.Exceptions;

namespace QLS.Backend.Controllers.Pricing;

[Route("api/v1/pricing")]
[ApiController]
public class PricingController : ControllerBase
{
    private readonly IPricingCalculatorService _pricingCalculatorService;

    public PricingController(IPricingCalculatorService pricingCalculatorService)
    {
        _pricingCalculatorService = pricingCalculatorService;
    }

    [HttpPost("calculate")]
    [AllowAnonymous] // Cho phép khách xem giá trước khi đăng nhập
    public async Task<IActionResult> Calculate([FromBody] CalculatePriceRequestDto dto)
    {
        var result = await _pricingCalculatorService.CalculatePriceAsync(dto);
        if (result == null) 
            throw new ApiException("Không tìm thấy bảng giá hoặc cấu hình giá phù hợp cho yêu cầu này", 400);
            
        return Ok(ApiResponse<PriceCalculationResponseDto>.Success(result));
    }
}
