using QLS.Backend.DTOs.Machine;

namespace QLS.Backend.Interfaces;

public interface IMachineSettingService
{
    /// <summary>Lấy cấu hình của một máy theo MachineId.</summary>
    Task<MachineSettingDto?> GetByMachineIdAsync(Guid machineId);

    /// <summary>
    /// Upsert: Nếu chưa có setting thì tạo mới, nếu đã có thì ghi đè toàn bộ.
    /// Trả về DTO sau khi lưu.
    /// </summary>
    Task<MachineSettingDto> UpsertAsync(Guid machineId, UpsertMachineSettingDto dto);

    /// <summary>Xoá cấu hình của máy. Trả về false nếu không tìm thấy.</summary>
    Task<bool> DeleteAsync(Guid machineId);
}
