using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs;
using QLS.Backend.DTOs.Loyalty;
using QLS.Backend.Exceptions;
using QLS.Backend.Interfaces.Loyalty;

namespace QLS.Backend.Controllers
{
    [ApiController]
    [Route("api/loyalty")]
    public class LoyaltyController : ControllerBase
    {
        private readonly ILoyaltyService _loyaltyService;
        private readonly IConfiguration _configuration;

        public LoyaltyController(ILoyaltyService loyaltyService, IConfiguration configuration)
        {
            _loyaltyService = loyaltyService;
            _configuration = configuration;
        }

        [Authorize(Roles = "LoyaltyCustomer")]
        [HttpPost("claim")]
        public async Task<IActionResult> Claim([FromBody] LoyaltyClaimRequestDto request)
        {
            var customerId = GetCustomerId();
            var result = await _loyaltyService.ClaimPointsAsync(customerId, request);
            return Ok(ApiResponse<LoyaltyClaimResponseDto>.Success(result, "Nhận điểm thành công"));
        }

        [Authorize(Roles = "LoyaltyCustomer")]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var customerId = GetCustomerId();
            var result = await _loyaltyService.GetMeAsync(customerId);
            return Ok(ApiResponse<LoyaltyMeResponseDto>.Success(result));
        }

        [Authorize(Roles = "LoyaltyCustomer")]
        [HttpGet("points/history")]
        public async Task<IActionResult> GetPointsHistory([FromQuery] int limit = 50)
        {
            var customerId = GetCustomerId();
            var result = await _loyaltyService.GetPointHistoryAsync(customerId, limit);
            return Ok(ApiResponse<IReadOnlyList<LoyaltyPointHistoryItemDto>>.Success(result));
        }

        [AllowAnonymous]
        [HttpGet("claim-link/{token}")]
        public IActionResult ResolveClaimLink(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ApiException("Token không hợp lệ.", 400);
            }

            var template = _configuration["Loyalty:MiniAppClaimUrlTemplate"];
            if (string.IsNullOrWhiteSpace(template))
            {
                template = "https://zalo.me/s/miniapp?claimToken={token}";
            }

            var targetUrl = template.Replace("{token}", Uri.EscapeDataString(token));
            return Redirect(targetUrl);
        }

        private Guid GetCustomerId()
        {
            var claim = User.FindFirst("LoyaltyCustomerId")?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(claim) || !Guid.TryParse(claim, out var customerId))
            {
                throw new UnauthorizedAccessException("Token loyalty không hợp lệ.");
            }

            return customerId;
        }
    }
}
