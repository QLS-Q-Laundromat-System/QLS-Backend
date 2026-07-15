using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using MQTTnet;
using MQTTnet.Client;
using QLS.Backend.Data;
using QLS.Backend.Interfaces;
using QLS.Backend.Models.Enums;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Models;
using QLS.Backend.Services.Machine;

namespace QLS.Backend.Services.Ziggbee;

public class MqttListenerService : BackgroundService
{
    // Cache tĩnh lưu các thiết bị Zigbee quét được, Key là storeCode
    public static readonly ConcurrentDictionary<string, List<DiscoveredZigbeeDevice>> DiscoveredDevices = new();

    public class DiscoveredZigbeeDevice
    {
        public string IeeeAddress { get; set; } = string.Empty;
        public string FriendlyName { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsCoordinator { get; set; }
    }

    public class Zigbee2MqttDevice
    {
        [JsonPropertyName("ieee_address")]
        public string IeeeAddress { get; set; } = string.Empty;

        [JsonPropertyName("friendly_name")]
        public string FriendlyName { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("definition")]
        public Zigbee2MqttDeviceDefinition? Definition { get; set; }
    }

    public class Zigbee2MqttDeviceDefinition
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("vendor")]
        public string Vendor { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }

    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MqttListenerService> _logger;
    private readonly string _mqttHost;
    private readonly int _mqttPort;
    private readonly string? _mqttUser;
    private readonly string? _mqttPass;
    private readonly IHardwareTrackerService _hardwareTracker;

    public MqttListenerService(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<MqttListenerService> logger, IHardwareTrackerService hardwareTracker)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
        _hardwareTracker = hardwareTracker;

        _mqttHost = configuration["Zigbee2Mqtt:Host"] ?? "localhost";
        _mqttPort = int.TryParse(configuration["Zigbee2Mqtt:Port"], out var port) ? port : 1883;
        _mqttUser = configuration["Zigbee2Mqtt:Username"];
        _mqttPass = configuration["Zigbee2Mqtt:Password"];
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttFactory();
        using var mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqttHost, _mqttPort)
            .WithClientId($"QLS-Listener-{Guid.NewGuid():N}")
            .WithCleanSession();

        if (!string.IsNullOrEmpty(_mqttUser))
        {
            options.WithCredentials(_mqttUser, _mqttPass);
        }

        mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

            // Kiểm tra xem có phải tin nhắn danh sách thiết bị từ bridge không
            if (topic.StartsWith("qls/") && topic.EndsWith("/bridge/devices"))
            {
                var parts = topic.Split('/');
                if (parts.Length >= 4)
                {
                    var storeCode = parts[1];
                    ProcessBridgeDevicesReport(storeCode, payload);
                }
                return;
            }

            _logger.LogInformation("📩 Nhận phản hồi MQTT: {Topic} -> {Payload}", topic, payload);
            await HandleHardwareResponseAsync(topic, payload);
        };

        mqttClient.DisconnectedAsync += async e =>
        {
            _logger.LogWarning("⚠️ MQTT Listener bị ngắt kết nối. Đang thử kết nối lại...");
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            try 
            { 
                if (!mqttClient.IsConnected)
                    await mqttClient.ConnectAsync(options.Build(), stoppingToken); 
            } 
            catch { }
        };

        try
        {
            await mqttClient.ConnectAsync(options.Build(), stoppingToken);
            
            // Lắng nghe topic chính của các thiết bị (Hỗ trợ cả chuẩn zigbee2mqtt local và qls/{storeCode} cloud)
            await mqttClient.SubscribeAsync("zigbee2mqtt/+");
            await mqttClient.SubscribeAsync("qls/+/+");
            await mqttClient.SubscribeAsync("qls/+/bridge/devices");
            
            _logger.LogInformation("✅ MQTT Listener đã kết nối và đang lắng nghe topics: zigbee2mqtt/+, qls/+/+, qls/+/bridge/devices");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Không thể kết nối MQTT Broker.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task HandleHardwareResponseAsync(string topic, string payload)
    {
        var parts = topic.Split('/');
        if (parts.Length < 2) return;

        string zigbeeId;
        string? storeCode = null;

        if (parts[0] == "qls" && parts.Length >= 3)
        {
            storeCode = parts[1];
            zigbeeId = parts[2];
        }
        else
        {
            zigbeeId = parts[1];
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var machineService = scope.ServiceProvider.GetRequiredService<IMachineService>();

        try
        {
            QLS.Backend.Models.Machine? machine = null;
            if (!string.IsNullOrEmpty(storeCode))
            {
                machine = await context.Machines
                    .Include(m => m.Store)
                    .FirstOrDefaultAsync(m => m.ZigbeeNetworkId == zigbeeId && m.Store.StoreId == storeCode);
            }
            else
            {
                machine = await context.Machines.FirstOrDefaultAsync(m => m.ZigbeeNetworkId == zigbeeId);
            }

            if (machine == null) return;

            var session = await context.MachineSessions
                .Where(s => s.MachineId == machine.Id && 
                           (s.Status == MachineSessionStatus.PaidWaitingForStart || s.Status == MachineSessionStatus.PendingPayment))
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (session == null) return;

            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            // 1. Kiểm tra thuộc tính Custom 'coin_status' (mới)
            if (root.TryGetProperty("coin_status", out var coinStatusProp))
            {
                var coinStatus = coinStatusProp.GetInt32();
                _logger.LogInformation("ℹ️ Nhận trạng thái coin_status={Status} từ thiết bị {ZigbeeId} cho Session {SessionId}", coinStatus, zigbeeId, session.Id);
                
                switch (coinStatus)
                {
                    case 0:
                        _hardwareTracker.UpdateStatus(session.Id, "Thiết bị sẵn sàng.");
                        break;
                    case 1:
                        _hardwareTracker.UpdateStatus(session.Id, "Đang nhả xu...");
                        break;
                    case 2:
                        _logger.LogInformation("💎 XÁC NHẬN: Thiết bị {ZigbeeId} đã hoàn thành nhả xu (coin_status=2) cho Session {SessionId}", zigbeeId, session.Id);
                        _hardwareTracker.UpdateStatus(session.Id, "Thiết bị đã nhả xu thành công.");
                        break;
                    case 3:
                        _logger.LogError("❌ THIẾT BỊ BÁO LỖI/BẬN (coin_status=3) trên {ZigbeeId}", zigbeeId);
                        _hardwareTracker.UpdateStatus(session.Id, "Lỗi thiết bị hoặc đang bận.");
                        await machineService.UpdateSessionStatusAsync(session.Id, MachineSessionStatus.Error, "Thiết bị báo lỗi hoặc đang bận bắn xu.");
                        break;
                }
            }
            // 2. Fallback: Kiểm tra thuộc tính 'brightness' (cho các thiết bị dùng firmware cũ)
            else if (root.TryGetProperty("brightness", out var brightnessProp))
            {
                var brightness = brightnessProp.GetByte();
                
                // Nếu brightness về 0, nghĩa là ESP32 đã kết thúc Task nhả xu
                if (brightness == 0)
                {
                    _logger.LogInformation("💎 XÁC NHẬN (Legacy): Thiết bị {ZigbeeId} đã hoàn thành nhả xu (Brightness=0) cho Session {SessionId}", zigbeeId, session.Id);
                    _hardwareTracker.UpdateStatus(session.Id, "Thiết bị đã nhả xu thành công.");
                }
            }
            // 3. Fallback: Hỗ trợ thêm trường 'result'
            else if (root.TryGetProperty("result", out var resultProp))
            {
                var result = resultProp.GetString();
                if (result == "success")
                {
                    _logger.LogInformation("💎 XÁC NHẬN (Explicit): Máy {ZigbeeId} đã báo thành công cho Session {SessionId}", zigbeeId, session.Id);
                    _hardwareTracker.UpdateStatus(session.Id, "Thiết bị đã nhả xu thành công.");
                }
                else if (result == "error")
                {
                    var errorMsg = root.TryGetProperty("message", out var m) ? m.GetString() : "Lỗi phần cứng";
                    _logger.LogError("❌ PHẦN CỨNG BÁO LỖI: {ZigbeeId} -> {Msg}", zigbeeId, errorMsg);
                    _hardwareTracker.UpdateStatus(session.Id, $"Lỗi thiết bị: {errorMsg}");
                    await machineService.UpdateSessionStatusAsync(session.Id, MachineSessionStatus.Error, $"Phần cứng báo lỗi: {errorMsg}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi xử lý phản hồi từ phần cứng.");
        }
    }

    private void ProcessBridgeDevicesReport(string storeCode, string payload)
    {
        try
        {
            var rawDevices = JsonSerializer.Deserialize<List<Zigbee2MqttDevice>>(payload);
            if (rawDevices == null) return;

            var list = new List<DiscoveredZigbeeDevice>();
            foreach (var dev in rawDevices)
            {
                if (dev.Type == "Coordinator") continue; // Bỏ qua bộ điều phối

                list.Add(new DiscoveredZigbeeDevice
                {
                    IeeeAddress = dev.IeeeAddress,
                    FriendlyName = dev.FriendlyName,
                    Model = dev.Definition?.Model ?? string.Empty,
                    Vendor = dev.Definition?.Vendor ?? string.Empty,
                    Description = dev.Definition?.Description ?? "Thiết bị Zigbee",
                    IsCoordinator = false
                });
            }

            DiscoveredDevices[storeCode] = list;
            _logger.LogInformation("📡 Đã cập nhật danh sách {Count} thiết bị quét được cho Store {StoreCode}", list.Count, storeCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi phân giải danh sách thiết bị từ bridge/devices của Store {StoreCode}", storeCode);
        }
    }
}
