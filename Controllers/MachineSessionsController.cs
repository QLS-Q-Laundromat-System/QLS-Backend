using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using QLS.Backend.Interfaces;
using QLS.Backend.DTOs.Machine;
using QLS.Backend.DTOs;
using System;
using System.Threading.Tasks;
using QLS.Backend.Interfaces.Loyalty;
using QLS.Backend.Data;
using QLS.Backend.Extensions;
using QLS.Backend.Exceptions;

namespace QLS.Backend.Controllers
{
    [ApiController]
    [Route("api/v1/sessions")]
    public class MachineSessionsController : ControllerBase
    {
        private readonly IMachineService _machineService;
        private readonly QLS.Backend.Services.Machine.IHardwareTrackerService _hardwareTracker;
        private readonly AppDbContext _context;

        public MachineSessionsController(
            IMachineService machineService,
            QLS.Backend.Services.Machine.IHardwareTrackerService hardwareTracker,
            AppDbContext context)
        {
            _machineService = machineService;
            _hardwareTracker = hardwareTracker;
            _context = context;
        }

        /// <summary>
        /// BƯỚC 1: Tính giá và khởi tạo session (chưa thu tiền).
        /// Server tự tính giá, lưu session với status PendingPayment.
        /// Client nhận SessionId và số tiền để đưa ra màn hình thanh toán.
        /// [POST] /api/v1/sessions/init
        /// </summary>
        [HttpPost("init")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> InitSession([FromBody] InitPaymentRequestDto dto)
        {
            try
            {
                dto.StoreId = User.GetRequiredStoreId();
                dto.UserId = User.GetRequiredUserId();
                var result = await _machineService.InitSessionAsync(dto);
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
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> ConfirmPayment(Guid id, [FromBody] ConfirmPaymentRequestDto dto)
        {
            try
            {
                var result = await _machineService.ConfirmPaymentAsync(id, dto.TransactionId);
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
        /// Hủy session trước khi thanh toán. Session đã thanh toán hoặc đang chạy
        /// không thể hủy bằng endpoint này.
        /// [POST] /api/v1/sessions/{id}/cancel
        /// </summary>
        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> CancelSession(Guid id)
        {
            try
            {
                await EnsureSessionAccessAsync(id);
                var result = await _machineService.CancelPendingSessionAsync(id);
                if (!result)
                    return NotFound(new { message = "Không tìm thấy session." });

                return Ok(ApiResponse<object>.Success(new { sessionId = id }, "Đã hủy session."));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// BƯỚC 3: Cập nhật trạng thái khi máy hoàn thành hoặc gặp sự cố.
        /// Running → Completed: Máy xong, doanh thu được ghi nhận.
        /// Running → Error: Máy lỗi giữa chừng, hệ thống đánh dấu cần hoàn tiền (RefundStatus=Pending).
        /// [PATCH] /api/v1/sessions/{id}/status
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateSessionStatusDto dto)
        {
            var result = await _machineService.UpdateSessionStatusAsync(id, dto.Status, dto.RefundNote);
            if (!result)
                return NotFound(new { message = "Không tìm thấy session." });

            return Ok(ApiResponse<object>.Success(new { }, "Cập nhật trạng thái thành công."));
        }

        /// <summary>
        /// Lấy chi tiết/trạng thái của một session để app có thể polling
        /// [GET] /api/v1/sessions/{id}
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> GetSessionStatus(
            Guid id,
            [FromServices] ILoyaltyService loyaltyService)
        {
            await EnsureSessionAccessAsync(id);
            var session = await _context.MachineSessions.FindAsync(id);
            if (session == null)
            {
                return NotFound(new { message = "Không tìm thấy session." });
            }

            var claimLinkBaseUrl = $"{Request.Scheme}://{Request.Host}/api/loyalty/claim-link";
            var loyalty = await loyaltyService.GetSessionLoyaltyInfoAsync(session.Id, claimLinkBaseUrl);

            return Ok(ApiResponse<object>.Success(new 
            { 
                sessionId = session.Id,
                status = session.Status,
                machineId = session.MachineId,
                hardwareStatus = _hardwareTracker.GetStatus(session.Id) ?? "Đang chờ thiết bị...",
                loyalty
            }));
        }

        private async Task EnsureSessionAccessAsync(Guid sessionId)
        {
            var storeId = await _context.MachineSessions
                .AsNoTracking()
                .Where(session => session.Id == sessionId)
                .Select(session => (Guid?)session.StoreId)
                .FirstOrDefaultAsync();

            if (!storeId.HasValue)
            {
                throw new ApiException("Không tìm thấy session.", 404);
            }

            await User.EnsureStoreAccessAsync(_context, storeId.Value);
        }
    }
}
