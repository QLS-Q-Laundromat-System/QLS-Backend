using System;

namespace QLS.Backend.Models
{
    public class UserAdmin
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty; 
        public string PasswordHash { get; set; } = string.Empty;
        
        // Phân quyền: "SuperAdmin" (Phe bạn), "Owner" (Chủ chuỗi), "Manager" (Quản lý chi nhánh), "Staff" (Nhân viên)
        public string Role { get; set; } = "Staff"; 
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // - Nếu Role là "SuperAdmin", trường này sẽ là null.
        // - Nếu Role là các quyền còn lại, trường này bắt buộc phải lưu ID của bảng Owner.
        public Guid? OwnerId { get; set; }
        public Owner? Owner { get; set; }

        // CÓ THỂ NULL: SuperAdmin và Owner không bị gán chết vào một chi nhánh nào cả
        public Guid? BranchId { get; set; }
        public Branch? Branch { get; set; }
    }
}
