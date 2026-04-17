using QLS.Backend.DTOs.Brand;
using QLS.Backend.DTOs.Store;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QLS.Backend.Interfaces.Brand
{
    public interface IBrandService
    {
        Task<List<BrandResponseDto>> GetAllBrandsAsync();
        Task<BrandResponseDto?> GetBrandByIdAsync(Guid id);
        Task<BrandResponseDto> CreateBrandAsync(CreateBrandDto dto);
        Task<BrandResponseDto> UpdateBrandAsync(Guid id, UpdateBrandDto dto);
        Task<List<BrandAdminDto>> GetAllBrandAdminsAsync();
        Task<bool> HasAccountAsync(Guid brandId);
        Task<List<StoreResponseDto>> GetStoresByBrandIdAsync(Guid brandId);
        Task<List<BrandAccountDto>> GetAccountsByBrandIdAsync(Guid brandId);
        Task<List<StoreTypeDto>> GetStoreTypesByBrandIdAsync(Guid brandId);
        Task<StoreTypeDto> CreateStoreTypeAsync(CreateStoreTypeDto dto);
    }
}
