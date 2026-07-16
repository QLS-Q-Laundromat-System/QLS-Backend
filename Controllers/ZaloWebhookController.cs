using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace QLS.Backend.Controllers
{
    [ApiController]
    [Route("api/webhooks/zalo")]
    public class ZaloWebhookController : ControllerBase
    {
        private readonly ILogger<ZaloWebhookController> _logger;

        public ZaloWebhookController(ILogger<ZaloWebhookController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult Receive([FromBody] JsonElement payload)
        {
            try
            {
                _logger.LogInformation("[Zalo Webhook] Received event with {PropertyCount} top-level properties.", payload.EnumerateObject().Count());

                // Điểm này có thể mở rộng xử lý bất đồng bộ các sự kiện như follow/unfollow/chat...
                // để luôn đảm bảo thời gian phản hồi dưới 2 giây theo yêu cầu của Zalo.

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Zalo Webhook] Lỗi khi xử lý sự kiện webhook từ Zalo.");
                // Trả về 200 OK kèm mã lỗi nội bộ để tránh Zalo tạm khóa Webhook khi gửi lỗi HTTP 500.
                return Ok(new { success = false });
            }
        }
    }
}
