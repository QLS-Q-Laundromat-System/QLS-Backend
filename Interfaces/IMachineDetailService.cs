using QLS.Backend.DTOs;
using QLS.Backend.DTOs.Machine;

namespace QLS.Backend.Services;

public interface IMachineDetailService
{
    Task<List<MachineDetailDto>> GetLgMachineStatusAsync(string storeId);
    Task<bool> UpdateMachineCapacityAsync(Guid machineId, string capacity);
    
    /// <summary>
    /// Lấy chi tiết máy bao gồm cấu hình (MachineSetting) và bảng giá hiện tại
    /// </summary>
    /// <param name="machineId">ID của máy</param>
    /// <returns>MachineDetailWithConfigDto chứa đủ thông tin máy, cấu hình và giá</returns>
    Task<MachineDetailWithConfigDto?> GetMachineDetailWithConfigAsync(Guid machineId);
}