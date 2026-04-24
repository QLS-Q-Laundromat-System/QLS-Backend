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
        /// BƯỚC 1: Tính giá và khởi tạo session (chưa thu tiền).
        /// Server tự tính giá, lưu session với status PendingPayment.
        /// Client nhận SessionId và số tiền để đưa ra màn hình thanh toán.
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

        /// <summary>
        /// BƯỚC 2: Xác nhận thanh toán thành công → chuyển PendingPayment → Running → Máy bắt đầu chạy.
        /// Gọi endpoint này sau khi payment gateway (VNPay/Momo/xu) xác nhận thu tiền thành công.
        /// Nếu máy lỗi sau bước này → dùng PATCH /status với Error để đánh dấu cần hoàn tiền.
        /// [POST] /api/v1/sessions/{id}/confirm-payment
        /// </summary>
        [HttpPost("{id}/confirm-payment")]
        public async Task<IActionResult> ConfirmPayment(Guid id, [FromBody] ConfirmPaymentRequestDto dto)
        {
            try
            {
                var result = await _dryerService.ConfirmPaymentAsync(id, dto.TransactionId);
                if (!result)
                    return NotFound(new { message = "Không tìm thấy session." });

                return Ok(ApiResponse<object>.Success(new { sessionId = id }, "Thanh toán xác nhận. Máy đang khởi động."));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// BƯỚC 3: Cập nhật trạng thái khi máy hoàn thành hoặc gặp sự cố.
        /// Running → Completed: Máy xong, doanh thu được ghi nhận.
        /// Running → Error: Máy lỗi giữa chừng, hệ thống đánh dấu cần hoàn tiền (RefundStatus=Pending).
        /// [PATCH] /api/v1/sessions/{id}/status
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateSessionStatusDto dto)
        {
            var result = await _dryerService.UpdateSessionStatusAsync(id, dto.Status, dto.RefundNote);
            if (!result)
                return NotFound(new { message = "Không tìm thấy session." });

            return Ok(ApiResponse<object>.Success(new { }, "Cập nhật trạng thái thành công."));
        }
    }
}
