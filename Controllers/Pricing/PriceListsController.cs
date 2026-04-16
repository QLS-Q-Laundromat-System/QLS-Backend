using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs;
using QLS.Backend.DTOs.Pricing;
using QLS.Backend.Interfaces.Pricing;
using QLS.Backend.Models.Enums;
using QLS.Backend.Exceptions;

namespace QLS.Backend.Controllers.Pricing;

[Route("api/v1/pricelists")]
[ApiController]
[Authorize]
public class PriceListsController : ControllerBase
{
    private readonly IPricingService _pricingService;

    public PriceListsController(IPricingService pricingService)
    {
        _pricingService = pricingService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PriceListStatus? status, [FromQuery] DateOnly? validFrom)
    {
        var brandIdStr = User.FindFirst("BrandId")?.Value;
        Guid? brandId = string.IsNullOrEmpty(brandIdStr) ? null : Guid.Parse(brandIdStr);

        var result = await _pricingService.GetPriceListsAsync(status, validFrom, brandId);
        return Ok(ApiResponse<IEnumerable<PriceListDto>>.Success(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var brandIdStr = User.FindFirst("BrandId")?.Value;
        Guid? brandId = string.IsNullOrEmpty(brandIdStr) ? null : Guid.Parse(brandIdStr);

        var result = await _pricingService.GetPriceListDetailAsync(id, brandId);
        if (result == null) throw new ApiException("Không tìm thấy bảng giá", 404);
        return Ok(ApiResponse<PriceListDetailDto>.Success(result));
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin,BrandAdmin")]
    public async Task<IActionResult> Create([FromBody] CreatePriceListDto dto)
    {
        var brandIdStr = User.FindFirst("BrandId")?.Value;
        if (!string.IsNullOrEmpty(brandIdStr))
        {
            dto.BrandId = Guid.Parse(brandIdStr);
        }

        var result = await _pricingService.CreatePriceListAsync(dto);
        return Ok(ApiResponse<PriceListDto>.Success(result, "Tạo bảng giá thành công"));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "SystemAdmin,BrandAdmin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdatePriceListStatusDto dto)
    {
        var brandIdStr = User.FindFirst("BrandId")?.Value;
        Guid? brandId = string.IsNullOrEmpty(brandIdStr) ? null : Guid.Parse(brandIdStr);

        var result = await _pricingService.UpdatePriceListStatusAsync(id, dto.Status, brandId);
        if (!result) throw new ApiException("Không tìm thấy bảng giá", 404);
        return Ok(ApiResponse<object>.Success(new { }, "Cập nhật trạng thái thành công"));
    }

    [HttpPost("{id}/store-types")]
    [Authorize(Roles = "SystemAdmin,BrandAdmin")]
    public async Task<IActionResult> AssignStoreTypes(Guid id, [FromBody] AssignPriceListStoreTypesDto dto)
    {
        var brandIdStr = User.FindFirst("BrandId")?.Value;
        Guid? brandId = string.IsNullOrEmpty(brandIdStr) ? null : Guid.Parse(brandIdStr);

        var result = await _pricingService.AssignStoreTypesAsync(id, dto, brandId);
        if (!result) throw new ApiException("Không tìm thấy bảng giá", 404);
        return Ok(ApiResponse<object>.Success(new { }, "Gán hạng cửa hàng thành công"));
    }

    [HttpPut("{id}/modes/per-kg")]
    [Authorize(Roles = "SystemAdmin,BrandAdmin")]
    public async Task<IActionResult> SyncPerKg(Guid id, [FromBody] List<PriceModePerKgItemDto> modes)
    {
        var brandIdStr = User.FindFirst("BrandId")?.Value;
        Guid? brandId = string.IsNullOrEmpty(brandIdStr) ? null : Guid.Parse(brandIdStr);

        var result = await _pricingService.SyncPriceModePerKgAsync(id, modes, brandId);
        if (!result) throw new ApiException("Không tìm thấy bảng giá", 404);
        return Ok(ApiResponse<object>.Success(new { }, "Đồng bộ giá theo Kg thành công"));
    }

    [HttpPut("{id}/modes/per-session")]
    [Authorize(Roles = "SystemAdmin,BrandAdmin")]
    public async Task<IActionResult> SyncPerSession(Guid id, [FromBody] List<PriceModePerSessionItemDto> modes)
    {
        var brandIdStr = User.FindFirst("BrandId")?.Value;
        Guid? brandId = string.IsNullOrEmpty(brandIdStr) ? null : Guid.Parse(brandIdStr);

        var result = await _pricingService.SyncPriceModePerSessionAsync(id, modes, brandId);
        if (!result) throw new ApiException("Không tìm thấy bảng giá", 404);
        return Ok(ApiResponse<object>.Success(new { }, "Đồng bộ giá theo lượt thành công"));
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate([FromBody] CalculatePriceRequestDto dto)
    {
        var result = await _pricingService.CalculatePriceAsync(dto);
        return Ok(ApiResponse<PriceCalculationResponseDto>.Success(result));
    }
}
