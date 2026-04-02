using System;

namespace QLS.Backend.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        // --- Thông tin cá nhân ---
        public string FullName { get; set; } = string.Empty;
        
        // Với App Mobile, Số điện thoại thường là định danh chính để đăng nhập/nhận OTP
        public string PhoneNumber { get; set; } = string.Empty; 
        
        // Email có thể không bắt buộc đối với khách hàng dùng app
        public string? Email { get; set; } 
        
        // --- Bảo mật ---
        // Nếu bạn định cho khách đăng nhập hoàn toàn bằng OTP (Zalo/SMS), trường này có thể bỏ trống
        public string? PasswordHash { get; set; } 
        
        // --- Thanh toán & Dịch vụ ---
        // Lưu số dư ví điện tử để khách nạp tiền trước và trừ dần khi quét mã QR giặt/sấy
        public decimal WalletBalance { get; set; } = 0; 
        
        // --- Trạng thái hệ thống ---
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // --- Quan hệ (Navigation Properties) ---
        // (Tạm thời để trống. Sau này khi làm tới chức năng Thanh toán/Lịch sử giặt, 
        // chúng ta sẽ thêm ICollection<MachineSession> vào đây để biết khách này đã giặt những đơn nào).
        public ICollection<MachineSession> MachineSessions { get; set; } = new List<MachineSession>();
    }
}
