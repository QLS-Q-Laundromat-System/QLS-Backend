using System.Text;
using System.Text.Json;
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
            
            // Lắng nghe topic chính của các thiết bị (Zigbee2MQTT mặc định publish vào zigbee2mqtt/DeviceName)
            await mqttClient.SubscribeAsync("zigbee2mqtt/+");
            
            _logger.LogInformation("✅ MQTT Listener đã kết nối và đang lắng nghe topic: zigbee2mqtt/+");
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
        var zigbeeId = parts[1];

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var machineService = scope.ServiceProvider.GetRequiredService<IMachineService>();

        try
        {
            var machine = await context.Machines.FirstOrDefaultAsync(m => m.ZigbeeNetworkId == zigbeeId);
            if (machine == null) return;

            var session = await context.MachineSessions
                .Where(s => s.MachineId == machine.Id && 
                           (s.Status == MachineSessionStatus.PaidWaitingForStart || s.Status == MachineSessionStatus.PendingPayment))
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (session == null) return;

            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            // Kiểm tra thuộc tính 'brightness' (đây là cách ESP32 báo cáo trạng thái hoàn thành)
            if (root.TryGetProperty("brightness", out var brightnessProp))
            {
                var brightness = brightnessProp.GetByte();
                
                // Nếu brightness về 0, nghĩa là ESP32 đã kết thúc Task nhả xu
                if (brightness == 0)
                {
                    _logger.LogInformation("💎 XÁC NHẬN: Thiết bị {ZigbeeId} đã hoàn thành nhả xu (Brightness=0) for Session {SessionId}", zigbeeId, session.Id);
                    _hardwareTracker.UpdateStatus(session.Id, "Thiết bị đã nhả xu thành công.");
                }
            }
            // Hỗ trợ thêm trường 'result' nếu sau này bạn nâng cấp ESP32
            else if (root.TryGetProperty("result", out var resultProp))
            {
                var result = resultProp.GetString();
                if (result == "success")
                {
                    _logger.LogInformation("💎 XÁC NHẬN (Explicit): Máy {ZigbeeId} đã báo thành công cho Session {SessionId}", zigbeeId, session.Id);
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
}
