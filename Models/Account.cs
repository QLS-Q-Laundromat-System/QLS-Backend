using System;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Models
{
    public class Account
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string Username { get; set; }

        public string PasswordHash { get; set; } = string.Empty;
        
        // Phân quyền: SuperAdmin, AdminBranch, Manager, Staff, Customer
        public UserRole Role { get; set; } = UserRole.Customer; 
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Liên kết phân cấp (Cho AdminBranch, Manager, Staff)
        public Guid? BrandId { get; set; }
        public Brand? Brand { get; set; }

        public Guid? StoreId { get; set; }
        public Store? Store { get; set; }

    }
}
