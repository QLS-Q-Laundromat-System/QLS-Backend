using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using QLS.Backend.Interfaces;
using QLS.Backend.DTOs.Dryer;

namespace QLS.Backend.Controllers
{
    // Cấu hình đường dẫn API. Ví dụ: /api/stores/STORE001/machines
    [Route("api/stores/{storeId}/machines")]
    [ApiController]
    public class DryersController : ControllerBase
    {
        private readonly IDryerService _dryerService;

        // Tiêm (Inject) Service vào Controller thông qua Interface
        public DryersController(IDryerService dryerService)
        {
            _dryerService = dryerService;
        }

        // Endpoint GET: /api/stores/{storeId}/machines/{machineId}/options?userId=abc
        [HttpGet("{machineId}/options")]
        public async Task<IActionResult> GetDryerOptions(string storeId, string machineId, [FromQuery] string userId)
        {
            // Kiểm tra đầu vào cơ bản
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { message = "Thiếu thông tin người dùng (userId)." });
            }

            try
            {
                // Gọi Service để xử lý logic "10 phút vàng"
                var options = await _dryerService.GetDryerOptionsAsync(storeId, machineId, userId);
                
                // Trả về HTTP 200 OK cùng với dữ liệu
                return Ok(options);
            }
            catch (System.Exception ex)
            {
                // Nếu có lỗi (ví dụ không tìm thấy cấu hình Store), trả về lỗi 400
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
