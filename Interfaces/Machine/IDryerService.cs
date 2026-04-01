using System.Threading.Tasks;
using QLS.Backend.DTOs.Dryer;

namespace QLS.Backend.Interfaces
{
    public interface IDryerService
    {
        Task<DryerOptionResponseDto> GetDryerOptionsAsync(string storeId, string machineId, string userId);
        Task SaveSessionAsync(string storeId, string machineId, string userId, int minutes);
    }
}
