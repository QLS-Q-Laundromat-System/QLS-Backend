using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs;
using QLS.Backend.DTOs.Zalo;
using QLS.Backend.Interfaces.Zalo;

namespace QLS.Backend.Controllers.Auth
{
    [ApiController]
    [Route("api/zalo/auth")]
    public class ZaloAuthController : ControllerBase
    {
        private readonly IZaloAuthService _zaloAuthService;

        public ZaloAuthController(IZaloAuthService zaloAuthService)
        {
            _zaloAuthService = zaloAuthService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] ZaloLoginRequestDto request)
        {
            var result = await _zaloAuthService.LoginAsync(request);
            return Ok(ApiResponse<ZaloLoginResponseDto>.Success(result, "Đăng nhập Zalo thành công"));
        }
    }
}
