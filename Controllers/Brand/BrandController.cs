using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs.Brand;
using QLS.Backend.DTOs.Store;
using QLS.Backend.Interfaces.Brand;
using System.Threading.Tasks;
using QLS.Backend.DTOs;
using System.Collections.Generic;
using System;
using QLS.Backend.Exceptions;
using QLS.Backend.Extensions;

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
            await User.EnsureBrandAccessAsync(id);
            var brand = await _brandService.GetBrandByIdAsync(id);
            if (brand == null) throw new ApiException("Không tìm thấy chủ chuỗi", 404);
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

        // 2.1 API Cập nhật một Chủ chuỗi
        [HttpPut("{id}")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> UpdateBrand(Guid id, [FromBody] UpdateBrandDto dto)
        {
            var updatedBrand = await _brandService.UpdateBrandAsync(id, dto);
            return Ok(ApiResponse<BrandResponseDto>.Success(updatedBrand, "Cập nhật chuỗi thành công"));
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
            await User.EnsureBrandAccessAsync(id);
            var stores = await _brandService.GetStoresByBrandIdAsync(id);
            return Ok(ApiResponse<IEnumerable<StoreResponseDto>>.Success(stores, "Lấy danh sách cửa hàng thành công"));
        }


        // 6. API Lấy danh sách các Account (tài khoản) thuộc về Chuỗi
        [HttpGet("{id}/accounts")]
        [Authorize(Roles = "SystemAdmin,BrandAdmin")]
        public async Task<IActionResult> GetAccountsByBrand(Guid id)
        {
            await User.EnsureBrandAccessAsync(id);
            var accounts = await _brandService.GetAccountsByBrandIdAsync(id);
            return Ok(ApiResponse<IEnumerable<BrandAccountDto>>.Success(accounts, "Lấy danh sách tài khoản thành công"));
        }

        [HttpGet("{id}/store-types")]
        [Authorize(Roles = "SystemAdmin,BrandAdmin")]
        public async Task<IActionResult> GetStoreTypesByBrand(Guid id)
        {
            await User.EnsureBrandAccessAsync(id);
            var storeTypes = await _brandService.GetStoreTypesByBrandIdAsync(id);
            return Ok(ApiResponse<IEnumerable<StoreTypeDto>>.Success(storeTypes, "Lấy danh sách hạng cửa hàng thành công"));
        }

        [HttpPost("{id}/store-types")]
        [Authorize(Roles = "SystemAdmin,BrandAdmin")]
        public async Task<IActionResult> CreateStoreType(Guid id, [FromBody] CreateStoreTypeDto dto)
        {
            await User.EnsureBrandAccessAsync(id);
            dto.BrandId = id;

            var result = await _brandService.CreateStoreTypeAsync(dto);
            return Ok(ApiResponse<StoreTypeDto>.Success(result, "Tạo hạng cửa hàng thành công"));
        }

        [HttpPut("store-types/{storeTypeId}")]
        [Authorize(Roles = "SystemAdmin,BrandAdmin")]
        public async Task<IActionResult> UpdateStoreType(Guid storeTypeId, [FromBody] UpdateStoreTypeInfoDto dto)
        {
            var brandIdStr = User.FindFirst("BrandId")?.Value;
            
            // If logged in as BrandAdmin, verify ownership
            if (!string.IsNullOrEmpty(brandIdStr))
            {
                var tokenBrandId = Guid.Parse(brandIdStr);
                var storeTypes = await _brandService.GetStoreTypesByBrandIdAsync(tokenBrandId);
                if (!storeTypes.Any(st => st.Id == storeTypeId))
                {
                    throw new ApiException("Bạn không có quyền cập nhật hạng cửa hàng này", 403);
                }
            }

            var result = await _brandService.UpdateStoreTypeAsync(storeTypeId, dto);
            if (result == null) throw new ApiException("Không tìm thấy hạng cửa hàng", 404);

            return Ok(ApiResponse<StoreTypeDto>.Success(result, "Cập nhật hạng cửa hàng thành công"));
        }

        [HttpDelete("store-types/{storeTypeId}")]
        [Authorize(Roles = "SystemAdmin,BrandAdmin")]
        public async Task<IActionResult> DeleteStoreType(Guid storeTypeId)
        {
            var brandIdStr = User.FindFirst("BrandId")?.Value;
            
            // If logged in as BrandAdmin, verify ownership
            if (!string.IsNullOrEmpty(brandIdStr))
            {
                var tokenBrandId = Guid.Parse(brandIdStr);
                var storeTypes = await _brandService.GetStoreTypesByBrandIdAsync(tokenBrandId);
                if (!storeTypes.Any(st => st.Id == storeTypeId))
                {
                    throw new ApiException("Bạn không có quyền xóa hạng cửa hàng này", 403);
                }
            }

            var result = await _brandService.DeleteStoreTypeAsync(storeTypeId);
            if (!result) throw new ApiException("Không tìm thấy hạng cửa hàng", 404);

            return Ok(ApiResponse<bool>.Success(true, "Xóa hạng cửa hàng thành công"));
        }
    }
}
