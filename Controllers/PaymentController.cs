using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs.Zigbee;
using QLS.Backend.Services;

namespace QLS.Backend.Controllers;

/// <summary>
/// Controller xử lý các hành động sau khi thanh toán thành công.
/// Hiện tại: bắn tín hiệu Zigbee để máy giặt nhận xu.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IZigbeeService _zigbeeService;

    public PaymentController(IZigbeeService zigbeeService)
    {
        _zigbeeService = zigbeeService;
    }

    /// <summary>
    /// [POST] api/payment/trigger-washer
    ///
    /// Gọi API này sau khi thanh toán thành công để kích hoạt máy giặt.
    /// Backend sẽ gửi tín hiệu MQTT → Zigbee2MQTT → ESP32 nhả xu đúng số bao.
    ///
    /// Body (JSON):
    /// {
    ///   "zigbeeDeviceTopic": "ESP32H2.Washer",   ← (Tùy chọn) tên thiết bị
    ///   "bagCount": 3                            ← số lần nhả xu (1–10)
    /// }
    /// </summary>
    [HttpPost("trigger-washer")]
    public async Task<ActionResult<TriggerWasherResponseDto>> TriggerWasher(
        [FromBody] TriggerWasherRequestDto request)
    {
        var topic = string.IsNullOrWhiteSpace(request.ZigbeeDeviceTopic) 
                    ? "QLS.Washer" 
                    : request.ZigbeeDeviceTopic;

        if (request.BagCount < 1 || request.BagCount > 20)
            return BadRequest(new TriggerWasherResponseDto
            {
                Success = false,
                Message = "BagCount phải nằm trong khoảng 1 đến 20."
            });

        return await ExecuteTrigger(topic, request.BagCount);
    }

    /// <summary>
    /// [GET] /pulse/3 
    /// 
    /// API SIÊU TỐC: Chỉ cần nhập số để bắn xu.
    /// Ví dụ: http://localhost:5078/pulse/3 -> Máy sẽ bắn 3 xu ngay lập tức.
    /// </summary>
    [HttpGet("/pulse/{count}")]
    public async Task<ActionResult<TriggerWasherResponseDto>> TriggerPulse(int count)
    {
        // Sử dụng topic mặc định là QLS.Washer theo code C của bạn
        return await ExecuteTrigger("QLS.Washer", count);
    }

    /// <summary>
    /// [GET] /trigger/{count} (Link cũ cho bạn nếu lỡ tay bấm)
    /// </summary>
    [HttpGet("/trigger/{count}")]
    public async Task<ActionResult<TriggerWasherResponseDto>> TriggerSuperSimple(int count)
    {
        return await ExecuteTrigger("QLS.Washer", count);
    }

    private async Task<ActionResult<TriggerWasherResponseDto>> ExecuteTrigger(string topic, int count)
    {
        // LOG CHẨN ĐOÁN (Chắc chắn sẽ thấy trên terminal của bạn)
        Console.WriteLine($"[DIAGNOSTIC] EXECUTING TRIGGER: topic={topic}, count={count}");

        // Nếu topic để trống hoặc là chuỗi mẫu "string" của Swagger thì dùng mặc định
        if (string.IsNullOrWhiteSpace(topic) || topic.Equals("string", StringComparison.OrdinalIgnoreCase))
        {
            topic = "QLS.Washer";
        }

        try
        {
            await _zigbeeService.TriggerAsync(topic, count);

            return Ok(new TriggerWasherResponseDto
            {
                Success           = true,
                Message           = $"Đã gửi lệnh nhả {count} xu tới máy '{topic}'.",
                ZigbeeDeviceTopic = topic,
                BagCount          = count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new TriggerWasherResponseDto
            {
                Success           = false,
                Message           = "Lỗi khi gửi tín hiệu Zigbee: " + ex.Message,
                ZigbeeDeviceTopic = topic,
                BagCount          = count
            });
        }
    }
}
