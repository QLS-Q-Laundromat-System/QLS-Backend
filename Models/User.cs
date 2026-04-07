using System;

namespace QLS.Backend.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        // --- Thông tin cá nhân ---
        public string FullName { get; set; } = string.Empty;
        
        // Với App Mobile, Số điện thoại thường là định danh chính để đăng nhập/nhận OTP
        public string Email { get; set; } = string.Empty; 
        
        // --- Trạng thái hệ thống ---
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // --- Quan hệ (Navigation Properties) ---
        public ICollection<MachineSession> MachineSessions { get; set; } = new List<MachineSession>();
    }
}
