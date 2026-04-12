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
        var result = await _pricingService.GetPriceListsAsync(status, validFrom);
        return Ok(ApiResponse<IEnumerable<PriceListDto>>.Success(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _pricingService.GetPriceListDetailAsync(id);
        if (result == null) throw new ApiException("Không tìm thấy bảng giá", 404);
        return Ok(ApiResponse<PriceListDetailDto>.Success(result));
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin,BrandAdmin")]
    public async Task<IActionResult> Create([FromBody] CreatePriceListDto dto)
    {
        var result = await _pricingService.CreatePriceListAsync(dto);
        return Ok(ApiResponse<PriceListDto>.Success(result, "Tạo bảng giá thành công"));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "SystemAdmin,BrandAdmin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdatePriceListStatusDto dto)
    {
        var result = await _pricingService.UpdatePriceListStatusAsync(id, dto.Status);
        if (!result) throw new ApiException("Không tìm thấy bảng giá", 404);
        return Ok(ApiResponse<object>.Success(new { }, "Cập nhật trạng thái thành công"));
    }

    [HttpPost("{id}/store-types")]
    [Authorize(Roles = "SystemAdmin,BrandAdmin")]
    public async Task<IActionResult> AssignStoreTypes(Guid id, [FromBody] AssignStoreTypeDto dto)
    {
        var result = await _pricingService.AssignStoreTypesAsync(id, dto);
        if (!result) throw new ApiException("Không tìm thấy bảng giá", 404);
        return Ok(ApiResponse<object>.Success(new { }, "Gán hạng cửa hàng thành công"));
    }

    [HttpPut("{id}/modes/per-kg")]
    [Authorize(Roles = "SystemAdmin,BrandAdmin")]
    public async Task<IActionResult> SyncPerKg(Guid id, [FromBody] List<PriceModePerKgItemDto> modes)
    {
        var result = await _pricingService.SyncPriceModePerKgAsync(id, modes);
        if (!result) throw new ApiException("Không tìm thấy bảng giá", 404);
        return Ok(ApiResponse<object>.Success(new { }, "Đồng bộ giá theo Kg thành công"));
    }

    [HttpPut("{id}/modes/per-session")]
    [Authorize(Roles = "SystemAdmin,BrandAdmin")]
    public async Task<IActionResult> SyncPerSession(Guid id, [FromBody] List<PriceModePerSessionItemDto> modes)
    {
        var result = await _pricingService.SyncPriceModePerSessionAsync(id, modes);
        if (!result) throw new ApiException("Không tìm thấy bảng giá", 404);
        return Ok(ApiResponse<object>.Success(new { }, "Đồng bộ giá theo lượt thành công"));
    }
}
