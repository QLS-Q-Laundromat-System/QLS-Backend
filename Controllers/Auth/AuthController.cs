using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs;
using QLS.Backend.Interfaces.Auth;
using QLS.Backend.Services; // Thêm using thư mục Services

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
            var token = await _authService.LoginAsync(request);

            if (token == null)
            {
                return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác!" });
            }

            return Ok(new 
            { 
                message = "Đăng nhập thành công",
                token = token 
            });
        }
    }
}