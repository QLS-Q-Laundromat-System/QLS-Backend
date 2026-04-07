using QLS.Backend.DTOs.Brand;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QLS.Backend.Interfaces.Brand
{
    public interface IBrandService
    {
        Task<List<BrandResponseDto>> GetAllBrandsAsync();
        Task<BrandResponseDto> CreateBrandAsync(CreateBrandDto dto);
        Task<List<BrandAdminDto>> GetAllBrandAdminsAsync();
    }
}
