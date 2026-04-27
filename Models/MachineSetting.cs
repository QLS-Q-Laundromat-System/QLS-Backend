using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLS.Backend.Models;

public class MachineSetting
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid MachineId { get; set; }

    [ForeignKey(nameof(MachineId))]
    public Machine? Machine { get; set; }

    // ─── PHẦN DÙNG CHUNG ──────────────────────────────────────────
    public int Coin { get; set; } // Map cho cả 'coin' (giặt) và 'coin1' (sấy)
    public int[] Price { get; set; } = []; // Map cho cả 'price' và 'regularPrice'
    

    // ─── PHẦN CỦA MÁY GIẶT (Để Nullable) ──────────────────────────
    public int[]? WashingTime { get; set; }
    public int[]? WaterLevel { get; set; }
    public int[]? RinsingTime { get; set; }
    public int[]? RinsingCount { get; set; }
    public string[]? SpinSpeed { get; set; }
    public string? TwinSpray { get; set; }
    public int? DropCount { get; set; }
    public bool? NonStopRinsing { get; set; }
    public int AddSuperWash { get; set; }

    // ─── PHẦN CỦA MÁY SẤY (Để Nullable) ───────────────────────────
    public int[]? DryCycleTime { get; set; }
    public int[]? TopOffTime { get; set; }
    public string? TopOff { get; set; }
    public string? SensingDry { get; set; }
    public int[]? TopOffPrice { get; set; }
    public double RatingMoney { get; set; } // Tỷ lệ quy đổi tiền (VD: 0.05)
}
