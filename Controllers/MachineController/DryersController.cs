using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using QLS.Backend.Interfaces;
using QLS.Backend.DTOs.Dryer;

namespace QLS.Backend.Controllers
{
    // Cấu hình đường dẫn API. Ví dụ: /api/stores/STORE001/machines
    [Route("api/branches/{branchId}/machines")]
    [ApiController]
    public class DryersController : ControllerBase
    {
        private readonly IDryerService _dryerService;

        // Tiêm (Inject) Service vào Controller thông qua Interface
        public DryersController(IDryerService dryerService)
        {
            _dryerService = dryerService;
        }

        // Endpoint GET: /api/branches/{branchId}/machines/{machineId}/options?userId=abc
        [HttpGet("{machineId}/options")]
        public async Task<IActionResult> GetDryerOptions(Guid branchId, string machineId, [FromQuery] Guid userId)
        {
            // Kiểm tra đầu vào cơ bản
            if (userId == Guid.Empty)
            {
                return BadRequest(new { message = "Thiếu thông tin người dùng (userId) hợp lệ." });
            }

            // Gọi Service để xử lý logic "10 phút vàng"
            var options = await _dryerService.GetDryerOptionsAsync(branchId, machineId, userId);
            
            // Trả về HTTP 200 OK cùng với dữ liệu
            return Ok(options);
        }
    }
}
