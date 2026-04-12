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
        public Guid MachineId { get; set; }

        [ForeignKey(nameof(MachineId))]
        public Machine? Machine { get; set; }

        [Required]
        public Guid UserId { get; set; } 

        [ForeignKey("UserId")]
        public User? User { get; set; }

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
