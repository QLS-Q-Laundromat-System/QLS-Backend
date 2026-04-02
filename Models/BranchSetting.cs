using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLS.Backend.Models
{
    public class BranchSetting
    {
        [Key]
        public int Id { get; set; }

        // Khóa ngoại liên kết với Chi nhánh
        [Required]
        public Guid BranchId { get; set; }

        [ForeignKey("BranchId")]
        public Branch? Branch { get; set; }

        // --- CÁC CẤU HÌNH CHO MÁY SẤY ---

        // 1. Số phút nhảy mỗi bước (Mặc định: 10 phút)
        public int DryerStepMinutes { get; set; } = 10; 

        // 2. Giá tiền cho mỗi bước nhảy (Mặc định: 13000 VNĐ)
        public decimal DryerStepPrice { get; set; } = 13000; 

        // 3. Số phút bắt buộc sấy lần đầu (Mặc định: 30 phút)
        public int DryerMinInitialMinutes { get; set; } = 30; 

        // 4. Thời gian "vàng" cho phép sấy tiếp tính từ lúc máy dừng (Mặc định: 10 phút)
        public int DryerGracePeriodMinutes { get; set; } = 10;
        
        // Sau này bạn có thêm máy giặt, máy ủi... thì cứ thêm cột vào đây
        // public decimal WasherPrice { get; set; }
    }
}
