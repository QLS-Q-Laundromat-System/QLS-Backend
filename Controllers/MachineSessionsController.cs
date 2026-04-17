using Microsoft.AspNetCore.Mvc;
using QLS.Backend.Interfaces;
using QLS.Backend.DTOs.Machine;
using QLS.Backend.DTOs;
using System;
using System.Threading.Tasks;

namespace QLS.Backend.Controllers
{
    [ApiController]
    [Route("api/v1/sessions")]
    public class MachineSessionsController : ControllerBase
    {
        private readonly IDryerService _dryerService;

        public MachineSessionsController(IDryerService dryerService)
        {
            _dryerService = dryerService;
        }

        /// <summary>
        /// Cập nhật trạng thái lượt giặt/sấy (ví dụ: Running -> Completed)
        /// [PATCH] /api/v1/sessions/{id}/status
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateSessionStatusDto dto)
        {
            var result = await _dryerService.UpdateSessionStatusAsync(id, dto.Status);
            if (!result)
            {
                return NotFound(new { message = "Không tìm thấy session." });
            }

            return Ok(ApiResponse<object>.Success(new { }, "Cập nhật trạng thái thành công."));
        }

        /// <summary>
        /// KHỞI TẠO THANH TOÁN (CHUẨN BẢO MẬT)
        /// Trả về số tiền TỰ TÍNH TRÊN SERVER và lưu Database thông qua Service.
        /// [POST] /api/v1/sessions/init
        /// </summary>
        [HttpPost("init")]
        public async Task<IActionResult> InitSession([FromBody] InitPaymentRequestDto dto)
        {
            try 
            {
                var result = await _dryerService.InitSessionAsync(dto);
                return Ok(ApiResponse<InitPaymentResponseDto>.Success(result));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
