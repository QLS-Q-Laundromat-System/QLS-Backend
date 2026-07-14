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
    /// <param name="storeCode">Mã định danh cửa hàng (không bắt buộc)</param>
    Task TriggerAsync(string zigbeeDeviceTopic, int bagCount, string? storeCode = null);

    /// <summary>
    /// Gửi lệnh đổi tên thiết bị thông qua MQTT Bridge của Zigbee2MQTT.
    /// </summary>
    /// <param name="storeCode">Mã định danh cửa hàng</param>
    /// <param name="oldFriendlyNameOrIeee">Địa chỉ IEEE cũ hoặc tên cũ</param>
    /// <param name="newFriendlyName">Tên mới</param>
    Task RenameDeviceAsync(string storeCode, string oldFriendlyNameOrIeee, string newFriendlyName);

    /// <summary>
    /// Gửi lệnh bật/tắt cho phép thiết bị mới kết nối vào mạng (Permit Join).
    /// </summary>
    /// <param name="storeCode">Mã định danh cửa hàng</param>
    /// <param name="permit">true để cho phép, false để khóa</param>
    /// <param name="durationSeconds">Thời gian mở cổng (giây)</param>
    Task SetPermitJoinAsync(string storeCode, bool permit, int durationSeconds = 120);
}
