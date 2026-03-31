namespace QLS.Backend.Services;

/// <summary>
/// Service giao tiếp với Zigbee2MQTT qua MQTT Broker.
/// </summary>
public interface IZigbeeService
{
    /// <summary>
    /// Gửi lệnh kích hoạt máy giặt với số bao chỉ định.
    /// Bắn signal MQTT → Zigbee2MQTT → ESP32 nhả đúng số xu.
    /// </summary>
    /// <param name="zigbeeDeviceTopic">Topic thiết bị trong Zigbee2MQTT</param>
    /// <param name="bagCount">Số bao (1–10)</param>
    Task TriggerAsync(string zigbeeDeviceTopic, int bagCount);
}
