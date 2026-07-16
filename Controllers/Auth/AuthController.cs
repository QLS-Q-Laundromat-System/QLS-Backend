using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using QLS.Backend.DTOs;
using QLS.Backend.Interfaces.Auth;
using QLS.Backend.Services;
using QLS.Backend.Models.Enums;
using QLS.Backend.Exceptions;

namespace QLS.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);

            if (response == null)
            {
                throw new ApiException("Email hoặc mật khẩu không chính xác!", 401);
            }

            return Ok(ApiResponse<LoginResponse>.Success(response, "Đăng nhập thành công"));
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);

            if (!result)
            {
                throw new ApiException("Tên đăng nhập đã tồn tại hoặc dữ liệu không hợp lệ.", 400);
            }

            return Ok(ApiResponse<object?>.Success(null, "Đăng ký tài khoản thành công"));
        }

        [HttpPost("create-account")]
        [Authorize]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
        {
            var userRoleStr = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var brandIdStr = User.FindFirst("BrandId")?.Value;

            if (!Enum.TryParse(userRoleStr, out UserRole creatorRole))
            {
                throw new ApiException("Không tìm thấy vai trò người dùng trong Token.", 401);
            }

            Guid? creatorBrandId = string.IsNullOrEmpty(brandIdStr) ? null : Guid.Parse(brandIdStr);

            var result = await _authService.CreateAdminAccountAsync(request, creatorRole, creatorBrandId);

            if (!result)
            {
                throw new ApiException("Tên đăng nhập đã tồn tại hoặc bạn không có quyền hạn tạo vai trò này.", 400);
            }

            return Ok(ApiResponse<object?>.Success(null, $"Tạo tài khoản {request.Role} thành công"));
        }
    }
}
