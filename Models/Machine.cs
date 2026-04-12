using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Models;

public class Machine
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid StoreId { get; set; }

    /// <summary>Tên hiển thị cho người dùng (Vd: Máy số 1, Máy số 2).</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // ─── NHÓM 1: GIAO TIẾP VỚI ĐÁM MÂY (LG CLOUD) ────────────────────────────

    /// <summary>
    /// Device ID của LG, dùng để gọi API LG Cloud (lấy trạng thái, điều khiển).
    /// Lấy từ API /devices sau khi có access_token.
    /// </summary>
    [MaxLength(100)]
    public string? LgDeviceId { get; set; }

    
    [MaxLength(17)]
    public string? Esp32MacAddress { get; set; }

    /// <summary>
    /// Mã mạng Zigbee (tùy chọn), dùng khi hệ thống có Zigbee Hub trung gian.
    /// </summary>
    [MaxLength(50)]
    public string? ZigbeeNetworkId { get; set; }

    // ─── CÁC THÔNG SỐ VẬT LÝ ──────────────────────────────────────────────────

    /// <summary>Loại máy: Giặt (Washer), Sấy (Dryer), hoặc Cả hai (Both).</summary>
    [Required]
    public MachineType Type { get; set; } = MachineType.Washer;

    /// <summary>Sức chứa của máy. Vd: "10kg", "15kg".</summary>
    [MaxLength(50)]
    public string Capacity { get; set; } = string.Empty;
}
