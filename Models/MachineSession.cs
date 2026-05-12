using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Models
{
    public class MachineSession
    {
        [Key]
        public Guid Id { get; set; } 

        [Required]
        public Guid MachineId { get; set; }

        [ForeignKey(nameof(MachineId))]
        public Machine? Machine { get; set; }

        /// <summary>
        /// StoreId denormalized for fast reporting.
        /// </summary>
        [Required]
        public Guid StoreId { get; set; }

        [ForeignKey(nameof(StoreId))]
        public Store? Store { get; set; }

        [Required]
        public Guid UserId { get; set; } 

        [ForeignKey("UserId")]
        public User? User { get; set; }

        // --- Pricing & Revenue ---
        
        [Precision(18, 2)]
        public decimal PricePaid { get; set; }

        [Precision(18, 2)]
        public decimal TaxAmount { get; set; }

        public int TotalMinutes { get; set; }

        // Liên kết đến Bảng giá đã áp dụng (để đối soát)
        public Guid? PriceListId { get; set; }
        
        [ForeignKey(nameof(PriceListId))]
        public PriceList? PriceList { get; set; }

        // Chế độ tính giá lúc bắt đầu (Theo Kg hay Theo Lượt)
        public PricePerType PricingMode { get; set; }

        // Phụ trợ: Cân nặng thực tế (nếu giặt sấy theo cân)
        [Precision(5, 2)]
        public decimal? WeightKg { get; set; }

        // Phụ trợ: Tên chu trình máy giặt (nếu có)
        [MaxLength(100)]
        public string? CycleName { get; set; }

        // Phụ trợ: Có phải là lượt sấy gia hạn (Extension) hay không?
        public bool IsExtension { get; set; } = false;

        // --- Timing ---

        // Thời điểm máy bắt đầu quay
        public DateTime StartTime { get; set; } 
        
        // Thời điểm máy sấy xong dự kiến (StartTime + TotalMinutes)
        public DateTime EndTime { get; set; } 
        
        // Thời điểm thực tế kết thúc (nếu khác với EndTime)
        public DateTime? ActualEndTime { get; set; }

        // --- Audit & Status ---

        public MachineSessionStatus Status { get; set; } = MachineSessionStatus.Running;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- Payment Confirmation ---

        /// <summary>
        /// Thời điểm thanh toán được xác nhận thành công (chuyển từ PendingPayment → Running).
        /// Null nếu session bị cancel trước khi thanh toán.
        /// </summary>
        public DateTime? PaymentConfirmedAt { get; set; }

        // --- External References ---
        
        public string? TransactionId { get; set; }
        
        /// <summary>
        /// Mã nội dung chuyển khoản (VD: QLS12345) dùng để đối soát tự động.
        /// </summary>
        [MaxLength(20)]
        public string? PaymentCode { get; set; }

        // --- Refund Tracking ---

        /// <summary>
        /// Trạng thái hoàn tiền. Null = chưa cần hoàn tiền.
        /// "Pending" = đang xử lý hoàn tiền, "Completed" = đã hoàn tiền xong.
        /// </summary>
        [MaxLength(20)]
        public string? RefundStatus { get; set; }

        /// <summary>
        /// Ghi chú lý do hoàn tiền (VD: "Máy gặp lỗi sau 3 phút hoạt động").
        /// </summary>
        [MaxLength(500)]
        public string? RefundNote { get; set; }
    }
}

