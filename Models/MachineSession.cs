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

        public int TotalMinutes { get; set; }

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

        // --- External References ---
        
        public string? TransactionId { get; set; }
    }
}

