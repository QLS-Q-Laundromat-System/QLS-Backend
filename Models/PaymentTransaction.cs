using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QLS.Backend.Models
{
    public class PaymentTransaction
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Liên kết đến Session nếu khớp mã.
        /// </summary>
        public Guid? MachineSessionId { get; set; }

        [ForeignKey(nameof(MachineSessionId))]
        public MachineSession? MachineSession { get; set; }

        [Required]
        [Precision(18, 2)]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "BankTransfer";

        /// <summary>
        /// Mã giao dịch từ Gateway (VD: mã giao dịch ngân hàng).
        /// </summary>
        [MaxLength(100)]
        public string? GatewayTransactionId { get; set; }

        /// <summary>
        /// Nội dung chuyển khoản thực tế nhận được.
        /// </summary>
        public string? TransactionContent { get; set; }

        /// <summary>
        /// Trạng thái: "Success", "Failed", "Pending", "MismatchAmount".
        /// </summary>
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Lưu toàn bộ JSON từ Webhook để phục vụ debug.
        /// </summary>
        public string? RawData { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
