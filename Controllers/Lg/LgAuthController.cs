using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs;
using QLS.Backend.DTOs.Lg;
using QLS.Backend.Interfaces.LG;
using QLS.Backend.Exceptions;
using System.Net;

namespace QLS.Backend.Controllers.Lg
{
    /// <summary>
    /// Controller xử lý xác thực và lấy token từ hệ thống LG ThinQ / Laundry API.
    /// </summary>
    [Route("api/lg/auth")]
    [ApiController]
    public class LgAuthController : ControllerBase
    {
        private readonly ILgAuthTokenService _lgAuthService;
        private readonly ILogger<LgAuthController> _logger;

        public LgAuthController(
            ILgAuthTokenService lgAuthService,
            ILogger<LgAuthController> logger)
        {
            _lgAuthService = lgAuthService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy LG access_token từ email và password tài khoản LG.
        /// </summary>
        [HttpPost("token")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<LgAuthTokenResult>), 200)]
        public async Task<IActionResult> GetLgToken([FromBody] LgLoginRequest request)
        {
            // ── Validate ────────────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ApiException("Email không được để trống.", 400);

            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ApiException("Password không được để trống.", 400);

            if (!request.Email.Contains('@'))
                throw new ApiException("Email không hợp lệ.", 400);

            // ── Thực thi LG flow ───────────────────────────────────────────────
            // Lưu ý: Các lỗi InvalidOperationException (từ Step 1-7) và HttpRequestException 
            // sẽ được GlobalExceptionMiddleware xử lý tự động.
            
            _logger.LogInformation("[LgAuth] Yêu cầu lấy token cho {Email}", request.Email);
            
            var result = await _lgAuthService.GetAccessTokenAsync(request);

            return Ok(ApiResponse<LgAuthTokenResult>.Success(
                result,
                $"Lấy LG access_token thành công cho {request.Email}"));
        }

        /// <summary>
        /// Làm mới LG access_token sử dụng refresh_token.
        /// </summary>
        [HttpPost("refresh")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<LgAuthTokenResult>), 200)]
        public async Task<IActionResult> RefreshToken([FromBody] LgRefreshRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                throw new ApiException("RefreshToken không được để trống.", 400);

            if (string.IsNullOrWhiteSpace(request.Oauth2BackendUrl))
                throw new ApiException("Oauth2BackendUrl không được để trống.", 400);

            _logger.LogInformation("[LgAuth] Yêu cầu làm mới token bằng RefreshToken");

            var result = await _lgAuthService.RefreshAccessTokenAsync(request.RefreshToken, request.Oauth2BackendUrl);

            return Ok(ApiResponse<LgAuthTokenResult>.Success(
                result,
                "Làm mới LG access_token thành công."));
        }

        /// <summary>
        /// [DEV] Kiểm tra SHA512 hash của password theo chuẩn LG sử dụng.
        /// </summary>
        [HttpGet("hash-check")]
        [Authorize]
        public IActionResult CheckHash([FromQuery] string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ApiException("Password không được để trống.", 400);

            var hash = _lgAuthService.HashPassword(password);
            return Ok(ApiResponse<object>.Success(new
            {
                original = password,
                sha512   = hash,
                length   = hash.Length
            }, "SHA512 hash của password"));
        }
    }
}
