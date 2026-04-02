using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs.Brand;
using QLS.Backend.Interfaces.Brand;
using System.Threading.Tasks;

namespace QLS.Backend.Controllers.Brand
{
    [Route("api/[controller]")]
    [ApiController]
    // BẮT BUỘC: Chỉ những người có Token hợp lệ VÀ có Role là "SuperAdmin" mới được vào
    [Authorize(Roles = "SuperAdmin")] 
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
            return Ok(brands);
        }

        // 2. API Tạo mới một Chủ chuỗi
        [HttpPost]
        public async Task<IActionResult> CreateBrand([FromBody] CreateBrandDto dto)
        {
            var newBrand = await _brandService.CreateBrandAsync(dto);
            return Ok(new 
            {
                message = "Tạo chủ chuỗi thành công",
                data = newBrand
            });
        }
    }
}
