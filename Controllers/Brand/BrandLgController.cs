using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs;
using QLS.Backend.DTOs.Lg;
using QLS.Backend.Interfaces.Brand;
using System;
using System.Threading.Tasks;

namespace QLS.Backend.Controllers.Brand
{
    [Route("api/brands/{brandId}/lg-auth")]
    [ApiController]
    [Authorize(Roles = "SystemAdmin")] // Chỉ SystemAdmin mới được cấu hình tài khoản LG cho Brand
    public class BrandLgController : ControllerBase
    {
        private readonly IBrandLgService _brandLgService;

        public BrandLgController(IBrandLgService brandLgService)
        {
            _brandLgService = brandLgService;
        }

        /// <summary>
        /// Liên kết tài khoản LG cho một Brand và lưu Access Token vào DB.
        /// </summary>
        [HttpPost("link")]
        public async Task<IActionResult> LinkAccount(Guid brandId, [FromBody] LgLoginRequest request)
        {
            var result = await _brandLgService.LinkLgAccountAsync(brandId, request);
            return Ok(ApiResponse<LgAuthTokenResult>.Success(
                result, 
                "Liên kết tài khoản LG và khởi tạo token thành công."));
        }

        /// <summary>
        /// Cập nhật (Refresh) token cho Brand từ thông tin đã lưu.
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(Guid brandId)
        {
            var result = await _brandLgService.RefreshBrandTokenAsync(brandId);
            return Ok(ApiResponse<LgAuthTokenResult>.Success(
                result, 
                "Cập nhật token mới thành công sử dụng Refresh Token."));
        }

        /// <summary>
        /// Đồng bộ danh sách Store từ LG ThinQ về database địa phương.
        /// </summary>
        [HttpPost("sync-stores")]
        public async Task<IActionResult> SyncStores(Guid brandId)
        {
            var count = await _brandLgService.SyncBrandStoresAsync(brandId);
            return Ok(ApiResponse<int>.Success(
                count, 
                $"Đã đồng bộ xong {count} cửa hàng mới từ LG ThinQ."));
        }
    }
}
