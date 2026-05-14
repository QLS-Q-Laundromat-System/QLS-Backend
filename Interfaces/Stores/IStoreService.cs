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
<<<<<<< Updated upstream
        Task<List<Machine>> GetMachinesByStoreIdAsync(Guid storeId);
=======
        Task<List<QLS.Backend.Models.Machine>> GetMachinesByStoreIdAsync(Guid storeId);
        Task<List<StoreMachineStatusDto>> GetMachinesWithStatusByStoreIdAsync(Guid storeId);
>>>>>>> Stashed changes
        Task<bool> AssignStoreTypeAsync(Guid storeId, Guid storeTypeId);
    }
}
