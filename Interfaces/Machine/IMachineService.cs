using System;
using System.Threading.Tasks;
using QLS.Backend.Models.Enums;
using QLS.Backend.DTOs.Machine;

namespace QLS.Backend.Interfaces
{
    public interface IMachineService
    {
        /// <summary>Lưu session mới với status PendingPayment, trả về sessionId.</summary>
        Task<Guid> SaveSessionAsync(CreateMachineSessionDto dto);

        /// <summary>Xác nhận thanh toán thành công → chuyển PendingPayment → Running.</summary>
        Task<bool> ConfirmPaymentAsync(Guid sessionId, string? transactionId = null);

        /// <summary>Cập nhật trạng thái session (Running→Completed/Error/Cancelled).</summary>
        Task<bool> UpdateSessionStatusAsync(Guid sessionId, MachineSessionStatus status, string? refundNote = null);

        Task<InitPaymentResponseDto> InitSessionAsync(InitPaymentRequestDto dto);
    }
}
