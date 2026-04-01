using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLS.Backend.Models
{
    public class MachineSession
    {
        [Key]
        public Guid Id { get; set; } 

        [Required]
        [MaxLength(50)]
        public string StoreId { get; set; } = string.Empty;

        [ForeignKey("StoreId")]
        public Store? Store { get; set; }

        [Required]
        [MaxLength(50)]
        public string MachineId { get; set; } = string.Empty;

        [ForeignKey("MachineId")]
        public Machine? Machine { get; set; }

        // Lưu ID của user (từ App) hoặc Mã thẻ cứng vật lý
        public string UserId { get; set; } = string.Empty; 

        // Thời điểm máy bắt đầu quay
        public DateTime StartTime { get; set; } 
        
        // Thời điểm máy sấy xong (StartTime + số phút khách mua)
        public DateTime EndTime { get; set; } 

        // Trạng thái phiên: 
        // 0 = Đang chạy (Running)
        // 1 = Đã xong (Finished)
        public int Status { get; set; } 
    }
}
