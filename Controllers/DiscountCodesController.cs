using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs.DiscountCode;
using QLS.Backend.Interfaces.DiscountCode;

namespace QLS.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiscountCodesController : ControllerBase
    {
        private readonly IDiscountCodeService _discountCodeService;

        public DiscountCodesController(IDiscountCodeService discountCodeService)
        {
            _discountCodeService = discountCodeService;
        }

        private Guid GetBrandId()
        {
            var brandIdClaim = User.FindFirst("BrandId")?.Value;
            if (string.IsNullOrEmpty(brandIdClaim) || !Guid.TryParse(brandIdClaim, out var brandId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập thông tin của Brand.");
            }
            return brandId;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Người dùng không hợp lệ.");
            }
            return userId;
        }

        [Authorize(Roles = "BrandAdmin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DiscountCodeCreateDto dto)
        {
            var brandId = GetBrandId();
            var result = await _discountCodeService.CreateAsync(brandId, dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [Authorize(Roles = "BrandAdmin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var brandId = GetBrandId();
            var results = await _discountCodeService.GetAllByBrandAsync(brandId);
            return Ok(results);
        }

        [Authorize(Roles = "BrandAdmin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var brandId = GetBrandId();
            var result = await _discountCodeService.GetByIdAsync(brandId, id);
            return Ok(result);
        }

        [Authorize(Roles = "BrandAdmin")]
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            var brandId = GetBrandId();
            var result = await _discountCodeService.GetOverviewAsync(brandId);
            return Ok(result);
        }

        [Authorize(Roles = "BrandAdmin")]
        [HttpGet("{id}/usages")]
        public async Task<IActionResult> GetUsageHistory(Guid id)
        {
            var brandId = GetBrandId();
            var results = await _discountCodeService.GetUsageHistoryAsync(brandId, id);
            return Ok(results);
        }

        [Authorize(Roles = "BrandAdmin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] DiscountCodeUpdateDto dto)
        {
            var brandId = GetBrandId();
            var result = await _discountCodeService.UpdateAsync(brandId, id, dto);
            return Ok(result);
        }

        [Authorize] // Allow users to validate code
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateCode([FromBody] ValidateDiscountRequestDto request)
        {
            var userId = GetUserId();
            var result = await _discountCodeService.ValidateCodeAsync(userId, request);
            if (!result.IsValid)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
