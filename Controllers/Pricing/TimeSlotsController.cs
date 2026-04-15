using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs;
using QLS.Backend.DTOs.Pricing;
using QLS.Backend.Interfaces.Pricing;

namespace QLS.Backend.Controllers.Pricing;

[Route("api/v1/timeslots")]
[ApiController]
[Authorize]
public class TimeSlotsController : ControllerBase
{
    private readonly IPricingService _pricingService;

    public TimeSlotsController(IPricingService pricingService)
    {
        _pricingService = pricingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var brandIdStr = User.FindFirst("BrandId")?.Value;
        Guid? brandId = string.IsNullOrEmpty(brandIdStr) ? null : Guid.Parse(brandIdStr);

        var result = await _pricingService.GetAllTimeSlotsAsync(brandId);
        return Ok(ApiResponse<IEnumerable<TimeSlotDto>>.Success(result));
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin,BrandAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateTimeSlotDto dto)
    {
        var brandIdStr = User.FindFirst("BrandId")?.Value;
        if (!string.IsNullOrEmpty(brandIdStr))
        {
            // Nếu là BrandAdmin, ép buộc dùng BrandId từ Token để tránh ghi đè lung tung
            dto.BrandId = Guid.Parse(brandIdStr);
        }

        var result = await _pricingService.CreateTimeSlotAsync(dto);
        return Ok(ApiResponse<TimeSlotDto>.Success(result, "Tạo khung giờ thành công"));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SystemAdmin,BrandAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateTimeSlotDto dto)
    {
        var brandIdStr = User.FindFirst("BrandId")?.Value;
        Guid? brandId = string.IsNullOrEmpty(brandIdStr) ? null : Guid.Parse(brandIdStr);

        if (brandId.HasValue)
        {
            dto.BrandId = brandId.Value;
        }

        var result = await _pricingService.UpdateTimeSlotAsync(id, dto, brandId);
        if (result == null) return NotFound(ApiResponse<object>.Error(404, "Không tìm thấy khung giờ"));
        return Ok(ApiResponse<TimeSlotDto>.Success(result, "Cập nhật khung giờ thành công"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SystemAdmin,BrandAdmin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var brandIdStr = User.FindFirst("BrandId")?.Value;
        Guid? brandId = string.IsNullOrEmpty(brandIdStr) ? null : Guid.Parse(brandIdStr);

        var result = await _pricingService.DeleteTimeSlotAsync(id, brandId);
        if (!result) return NotFound(ApiResponse<object>.Error(404, "Không tìm thấy khung giờ"));
        return Ok(ApiResponse<object>.Success(new { }, "Xóa/Vô hiệu hóa khung giờ thành công"));
    }
}
