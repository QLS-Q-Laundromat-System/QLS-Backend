using System.Threading.Tasks;
using QLS.Backend.DTOs.Dryer;

namespace QLS.Backend.Interfaces
{
    public interface IDryerService
    {
        Task<DryerOptionResponseDto> GetDryerOptionsAsync(Guid branchId, Guid machineId, Guid userId);
        Task SaveSessionAsync(Guid branchId, Guid machineId, Guid userId, int minutes, decimal pricePaid);
    }
}

