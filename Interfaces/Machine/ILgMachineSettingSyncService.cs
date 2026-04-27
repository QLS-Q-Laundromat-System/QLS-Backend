using QLS.Backend.DTOs.Machine;

namespace QLS.Backend.Interfaces;

/// <summary>
/// Lấy setting của một machine từ DB.
/// Nếu chưa có setting, sẽ tự động gọi LG API để lấy và lưu vào DB.
/// </summary>
public interface ILgMachineSettingSyncService
{
    /// <summary>
    /// Trả về setting của máy.
    /// - Nếu DB đã có → trả về ngay (không gọi LG).
    /// - Nếu chưa có → gọi LG API, parse, upsert DB, rồi trả về.
    /// </summary>
    Task<MachineSettingDto> GetOrFetchSettingAsync(Guid machineId);

    /// <summary>Cập nhật cấu hình xuống DB VÀ đẩy lên LG server.</summary>
    Task<MachineSettingDto> UpdateAndSyncSettingAsync(Guid machineId, UpsertMachineSettingDto dto);
}
