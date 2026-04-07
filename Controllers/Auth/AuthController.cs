using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using QLS.Backend.DTOs;
using QLS.Backend.Interfaces.Auth;
using QLS.Backend.Services; // Thêm using thư mục Services
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        // Tiêm (Inject) IAuthService vào thay vì AppDbContext
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Chuyển toàn bộ logic xuống Service xử lý
            var response = await _authService.LoginAsync(request);

            if (response == null)
            {
                return Unauthorized(ApiResponse<object>.Error(401, "Email hoặc mật khẩu không chính xác!"));
            }

            return Ok(ApiResponse<LoginResponse>.Success(response, "Đăng nhập thành công"));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);

            if (!result)
            {
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại hoặc dữ liệu không hợp lệ." });
            }

            return Ok(new { message = "Đăng ký tài khoản thành công" });
        }

        [HttpPost("create-account")]
        [Authorize] // Phân quyên phân cấp: Chỉ những ai có Token hợp lệ
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
        {
            // 1. Trích xuất thông tin người tạo từ Token
            var userRoleStr = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var brandIdStr = User.FindFirst("BrandId")?.Value;

            if (!Enum.TryParse(userRoleStr, out UserRole creatorRole))
            {
                return Unauthorized(new { message = "Không tìm thấy vai trò người dùng trong Token." });
            }

            Guid? creatorBrandId = string.IsNullOrEmpty(brandIdStr) ? null : Guid.Parse(brandIdStr);

            // 2. Chuyển sang Service xử lý kèm theo bối cảnh người tạo
            var result = await _authService.CreateAdminAccountAsync(request, creatorRole, creatorBrandId);

            if (!result)
            {
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại hoặc bạn không có quyên hạn tạo vai trò này." });
            }

            return Ok(new { message = $"Tạo tài khoản {request.Role} thành công" });
        }
    }
}