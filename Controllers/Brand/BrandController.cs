using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs.Brand;
using QLS.Backend.DTOs.Store;
using QLS.Backend.Interfaces.Brand;
using System.Threading.Tasks;
using QLS.Backend.DTOs;
using System.Collections.Generic;
using System;

namespace QLS.Backend.Controllers.Brand
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Yêu cầu đăng nhập chung
    public class BrandController : ControllerBase
    {
        private readonly IBrandService _brandService;

        public BrandController(IBrandService brandService)
        {
            _brandService = brandService;
        }

        // 1. API Lấy danh sách toàn bộ Chủ chuỗi
        [HttpGet]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> GetAllBrands()
        {
            var brands = await _brandService.GetAllBrandsAsync();
            return Ok(ApiResponse<IEnumerable<BrandResponseDto>>.Success(brands, "Lấy danh sách thành công"));
        }

        // 1.1 API Lấy chi tiết một Chủ chuỗi
        [HttpGet("{id}")]
        [Authorize(Roles = "SystemAdmin,BrandAdmin")]
        public async Task<IActionResult> GetBrandById(Guid id)
        {
            var brand = await _brandService.GetBrandByIdAsync(id);
            if (brand == null) return NotFound(ApiResponse<object>.Error(404, "Không tìm thấy chủ chuỗi"));
            return Ok(ApiResponse<BrandResponseDto>.Success(brand, "Lấy thông tin thành công"));
        }

        [HttpGet("{id}/has-account")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> CheckHasAccount(Guid id)
        {
            var hasAccount = await _brandService.HasAccountAsync(id);
            return Ok(ApiResponse<bool>.Success(hasAccount, hasAccount ? "Chuỗi đã có tài khoản" : "Chuỗi chưa có tài khoản"));
        }

        // 2. API Tạo mới một Chủ chuỗi
        [HttpPost]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> CreateBrand([FromBody] CreateBrandDto dto)
        {
            var newBrand = await _brandService.CreateBrandAsync(dto);
            return Ok(ApiResponse<BrandResponseDto>.Success(newBrand, "Tạo chủ chuỗi thành công"));
        }

        // 3. API Lấy danh sách các tài khoản Admin của các Chuỗi
        [HttpGet("admins")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> GetAllBrandAdmins()
        {
            var admins = await _brandService.GetAllBrandAdminsAsync();
            return Ok(ApiResponse<IEnumerable<BrandAdminDto>>.Success(admins, "Lấy danh sách admin thành công"));
        }

        // 4. API Lấy danh sách các Store của một Chuỗi
        [HttpGet("{id}/stores")]
        [Authorize(Roles = "SystemAdmin,BrandAdmin")]
        public async Task<IActionResult> GetStoresByBrand(Guid id)
        {
            var stores = await _brandService.GetStoresByBrandIdAsync(id);
            return Ok(ApiResponse<IEnumerable<StoreResponseDto>>.Success(stores, "Lấy danh sách cửa hàng thành công"));
        }

        // 5. API Lấy danh sách các Account (tài khoản) thuộc về Chuỗi
        [HttpGet("{id}/accounts")]
        [Authorize(Roles = "SystemAdmin,BrandAdmin")]
        public async Task<IActionResult> GetAccountsByBrand(Guid id)
        {
            var accounts = await _brandService.GetAccountsByBrandIdAsync(id);
            return Ok(ApiResponse<IEnumerable<BrandAccountDto>>.Success(accounts, "Lấy danh sách tài khoản thành công"));
        }
    }
}
