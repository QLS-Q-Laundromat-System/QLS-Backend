using System.Threading.Tasks;
using QLS.Backend.DTOs.Dryer;
using QLS.Backend.Models.Enums;
using QLS.Backend.DTOs.Machine;

namespace QLS.Backend.Interfaces
{
    public interface IDryerService
    {
        Task<DryerOptionResponseDto> GetDryerOptionsAsync(Guid branchId, Guid machineId, Guid userId);
        Task SaveSessionAsync(CreateMachineSessionDto dto);
            
        Task<bool> UpdateSessionStatusAsync(Guid sessionId, MachineSessionStatus status);
        Task<InitPaymentResponseDto> InitSessionAsync(InitPaymentRequestDto dto);
    }
}

