using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;

namespace QLS.Backend.Services;

/// <summary>
/// Giao tiếp với Zigbee2MQTT qua MQTT Broker để điều khiển relay máy giặt.
///
/// Luồng hoạt động khi thanh toán thành công:
///   Backend (POST /api/payment/trigger-washer)
///     → ZigbeeService.TriggerAsync()
///     → MQTT Broker (localhost:1883)
///     → Zigbee2MQTT
///     → ESP32-H2 (nhả xu đúng số bao)
///
/// Payload MQTT gửi đi:
///   Topic  : zigbee2mqtt/{zigbeeDeviceTopic}/set
///   Message: { "state": "ON", "brightness": bagCount }
///     - "state: ON"      → ESP32 kích relay
///     - "brightness"     → số bao (1–10), ESP32 đọc và quyết định số lần nhả xu
///
/// LƯU Ý: ESP32 cần firmware hỗ trợ Dimmable Light (Level Control cluster).
/// </summary>
public class ZigbeeService : IZigbeeService
{
    private readonly string _mqttHost;
    private readonly int    _mqttPort;
    private readonly string? _mqttUser;
    private readonly string? _mqttPass;
    private readonly ILogger<ZigbeeService> _logger;

    public ZigbeeService(IConfiguration configuration, ILogger<ZigbeeService> logger)
    {
        _mqttHost = configuration["Zigbee2Mqtt:Host"] ?? "localhost";
        _mqttPort = int.TryParse(configuration["Zigbee2Mqtt:Port"], out var port) ? port : 1883;
        _mqttUser = configuration["Zigbee2Mqtt:Username"];
        _mqttPass = configuration["Zigbee2Mqtt:Password"];
        _logger   = logger;
    }

    /// <inheritdoc/>
    public async Task TriggerAsync(string zigbeeDeviceTopic, int bagCount)
    {
        if (bagCount < 1)  bagCount = 1;
        if (bagCount > 20) bagCount = 20;

        var topic = $"zigbee2mqtt/{zigbeeDeviceTopic}/set";

        // Gộp set số bao (brightness) + bật ON trong một lần publish duy nhất
        var payload = JsonSerializer.Serialize(new
        {
            state      = "ON",
            brightness = bagCount  // ESP32 đọc Level Control attribute → số lần nhả xu
        });

        _logger.LogInformation(
            "[ZigbeeService] TRIGGER → topic={Topic} | payload={Payload}",
            topic, payload);

        await PublishAsync(topic, payload);
    }

    // ---------- Private helper ----------

    private async Task PublishAsync(string topic, string payload)
    {
        var factory = new MqttFactory();
        using var client = factory.CreateMqttClient();

        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqttHost, _mqttPort)
            .WithClientId($"QLS-Backend-{Guid.NewGuid():N}")
            .WithCleanSession();

        if (!string.IsNullOrEmpty(_mqttUser))
        {
            optionsBuilder.WithCredentials(_mqttUser, _mqttPass);
        }

        var options = optionsBuilder.Build();

        await client.ConnectAsync(options, CancellationToken.None);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(payload))
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(false)
            .Build();

        await client.PublishAsync(message, CancellationToken.None);
        await client.DisconnectAsync();
    }
}
