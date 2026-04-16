using QLS.Backend.DTOs;

namespace QLS.Backend.Services;

public interface IMachineDetailService
{
    Task<List<MachineDetailDto>> GetLgMachineStatusAsync(string storeId);
    Task<bool> UpdateMachineCapacityAsync(Guid machineId, string capacity);
}