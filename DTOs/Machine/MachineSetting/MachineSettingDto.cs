namespace QLS.Backend.DTOs.Machine;

/// <summary>Response DTO – trả về cấu hình của máy.</summary>
public class MachineSettingDto
{
    public Guid Id { get; set; }
    public Guid MachineId { get; set; }

    // ─── PHẦN DÙNG CHUNG ──────────────────────────────────────────
    public int Coin { get; set; }
    public int[] Price { get; set; } = [];
    

    // ─── PHẦN CỦA MÁY GIẶT ────────────────────────────────────────
    public int[]? WashingTime { get; set; }
    public int[]? WaterLevel { get; set; }
    public int[]? RinsingTime { get; set; }
    public int[]? RinsingCount { get; set; }
    public string[]? SpinSpeed { get; set; }
    public string? TwinSpray { get; set; }
    public int? DropCount { get; set; }
    public bool? NonStopRinsing { get; set; }
    public int AddSuperWash { get; set; }

    // ─── PHẦN CỦA MÁY SẤY ────────────────────────────────────────
    public int[]? DryCycleTime { get; set; }
    public int[]? TopOffTime { get; set; }
    public string? TopOff { get; set; }
    public string? SensingDry { get; set; }
    public int[]? TopOffPrice { get; set; }
}
