using QLS.Backend.DTOs;

namespace QLS.Backend.Services;

public interface IMachineDetailService
{
    Task<List<MachineDetailDto>> GetLgMachineStatusAsync(Guid storeId);
}