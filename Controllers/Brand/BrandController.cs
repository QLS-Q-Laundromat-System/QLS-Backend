using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs.Brand;
using QLS.Backend.Interfaces.Brand;
using System.Threading.Tasks;
using QLS.Backend.DTOs;
using System.Collections.Generic;

namespace QLS.Backend.Controllers.Brand
{
    [Route("api/[controller]")]
    [ApiController]
    // BẮT BUỘC: Chỉ những người có Token hợp lệ VÀ có Role là "SystemAdmin" mới được vào
    [Authorize(Roles = "SystemAdmin")] 
    public class BrandController : ControllerBase
    {
        private readonly IBrandService _brandService;

        public BrandController(IBrandService brandService)
        {
            _brandService = brandService;
        }

        // 1. API Lấy danh sách toàn bộ Chủ chuỗi
        [HttpGet]
        public async Task<IActionResult> GetAllBrands()
        {
            var brands = await _brandService.GetAllBrandsAsync();
            return Ok(ApiResponse<IEnumerable<BrandResponseDto>>.Success(brands, "Lấy danh sách thành công"));
        }

        // 2. API Tạo mới một Chủ chuỗi
        [HttpPost]
        public async Task<IActionResult> CreateBrand([FromBody] CreateBrandDto dto)
        {
            var newBrand = await _brandService.CreateBrandAsync(dto);
            return Ok(ApiResponse<BrandResponseDto>.Success(newBrand, "Tạo chủ chuỗi thành công"));
        }

        // 3. API Lấy danh sách các tài khoản Admin của các Chuỗi
        [HttpGet("admins")]
        public async Task<IActionResult> GetAllBrandAdmins()
        {
            var admins = await _brandService.GetAllBrandAdminsAsync();
            return Ok(ApiResponse<IEnumerable<BrandAdminDto>>.Success(admins, "Lấy danh sách admin thành công"));
        }
    }
}
