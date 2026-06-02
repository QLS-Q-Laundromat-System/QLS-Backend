using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs;
using QLS.Backend.DTOs.Loyalty.Auth;
using QLS.Backend.Interfaces.Loyalty;

namespace QLS.Backend.Controllers.Auth
{
    [ApiController]
    [Route("api/loyalty/auth")]
    public class LoyaltyAuthController : ControllerBase
    {
        private readonly ILoyaltyAuthService _loyaltyAuthService;

        public LoyaltyAuthController(ILoyaltyAuthService loyaltyAuthService)
        {
            _loyaltyAuthService = loyaltyAuthService;
        }

        [HttpPost("otp/request")]
        public async Task<IActionResult> RequestOtp([FromBody] LoyaltyOtpRequestDto request)
        {
            var result = await _loyaltyAuthService.RequestOtpAsync(request);
            return Ok(ApiResponse<LoyaltyOtpRequestResponseDto>.Success(result, "Đã gửi mã OTP"));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] LoyaltyRegisterRequestDto request)
        {
            var result = await _loyaltyAuthService.RegisterAsync(request);
            return Ok(ApiResponse<LoyaltyAuthResponseDto>.Success(result, "Đăng ký thành công"));
        }

        [HttpPost("login/password")]
        public async Task<IActionResult> LoginWithPassword([FromBody] LoyaltyPasswordLoginRequestDto request)
        {
            var result = await _loyaltyAuthService.LoginWithPasswordAsync(request);
            return Ok(ApiResponse<LoyaltyAuthResponseDto>.Success(result, "Đăng nhập thành công"));
        }

        [HttpPost("login/otp")]
        public async Task<IActionResult> LoginWithOtp([FromBody] LoyaltyOtpLoginRequestDto request)
        {
            var result = await _loyaltyAuthService.LoginWithOtpAsync(request);
            return Ok(ApiResponse<LoyaltyAuthResponseDto>.Success(result, "Đăng nhập thành công"));
        }
    }
}
