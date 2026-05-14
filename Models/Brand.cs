using System;
using System.Collections.Generic;

namespace QLS.Backend.Models
{
    public class Brand
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public string? Logo { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Store> Stores { get; set; } = new List<Store>();
        public ICollection<Account> Accounts { get; set; } = new List<Account>();
        public ICollection<StoreType> StoreTypes { get; set; } = new List<StoreType>();

        // Navigation: thông tin xác thực LG OAuth (1 Brand - 1 tài khoản LG)
        public BrandLgCredential? LgCredential { get; set; }

        // --- Cấu hình Thanh toán ---
        public ICollection<PaymentConfig> PaymentConfigs { get; set; } = new List<PaymentConfig>();
    }
}
