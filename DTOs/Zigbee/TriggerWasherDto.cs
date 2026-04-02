using System.Text.Json.Serialization;

namespace QLS.Backend.DTOs.Zigbee;

/// <summary>
/// Request body khi thanh toán thành công —
/// gửi tín hiệu Zigbee để máy giặt nhận xu.
/// </summary>
public class TriggerWasherRequestDto
{
    /// <summary>
    /// Topic Zigbee2MQTT của thiết bị (tên hoặc IEEE address).
    /// Ví dụ: "0x00124b0014ab12cd"  hoặc  "ESP32H2.Washer"
    /// </summary>
    public string ZigbeeDeviceTopic { get; set; } = string.Empty;

    /// <summary>
    /// Số bao cần giặt (1–10). Mỗi bao = 1 lần nhả xu trên relay.
    /// </summary>
    public int BagCount { get; set; } = 1;
}

public class TriggerWasherResponseDto
{
    public bool   Success           { get; set; }
    public string Message           { get; set; } = string.Empty;
    public string ZigbeeDeviceTopic { get; set; } = string.Empty;
    public int    BagCount          { get; set; }
}
