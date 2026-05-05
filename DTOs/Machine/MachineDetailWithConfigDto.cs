using QLS.Backend.DTOs.Machine;
using QLS.Backend.DTOs.Pricing;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.DTOs.Machine;

/// <summary>
/// Response DTO - Trả về chi tiết máy với cấu hình (MachineSetting) + Bảng giá
/// </summary>
public class MachineDetailWithConfigDto
{
    // ─── THÔNG TIN MÁY CƠ BẢN ─────────────────────────────────────
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LgDeviceId { get; set; }
    public string? Esp32MacAddress { get; set; }
    public string? ZigbeeNetworkId { get; set; }
    public MachineType Type { get; set; }
    public string Capacity { get; set; } = string.Empty;

    // ─── CẤU HÌNH MÁY (MachineSetting) ────────────────────────────
    public MachineSettingDto? Setting { get; set; }

    // ─── CẤU HÌNH GIÁ (PriceList của Store này) ───────────────────
    public PriceListDetailDto? CurrentPriceList { get; set; }

    // ─── THÔNG TIN TRẠNG THÁI (TỪ LG NẾU CÓ) ──────────────────────
    public string? CurrentStatus { get; set; }
    public bool IsOnline { get; set; }
    public string? RemainTime { get; set; }
    public string? CurrentCourse { get; set; }
}
