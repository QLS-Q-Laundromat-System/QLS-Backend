using QLS.Backend.DTOs.Store;
using QLS.Backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QLS.Backend.Interfaces.Stores
{
    public interface IStoreService
    {
        Task<IEnumerable<Store>> GetStoresAsync();
        Task<StoreResponseDto> GetStoreByIdAsync(Guid id);
        Task<int> GetStoreCountAsync();
        Task<StoreResponseDto> CreateStoreAsync(CreateStoreDto dto);
        Task<StoreResponseDto> UpdateStoreAsync(Guid id, UpdateStoreDto dto);
        Task<List<StoreAccountDto>> GetAccountsByStoreIdAsync(Guid storeId);
    }
}
