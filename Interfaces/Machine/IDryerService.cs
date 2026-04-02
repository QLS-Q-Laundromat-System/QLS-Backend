using System.Threading.Tasks;
using QLS.Backend.DTOs.Dryer;

namespace QLS.Backend.Interfaces
{
    public interface IDryerService
    {
        Task<DryerOptionResponseDto> GetDryerOptionsAsync(Guid branchId, string machineId, Guid userId);
        Task SaveSessionAsync(Guid branchId, string machineId, Guid userId, int minutes);
    }
}
