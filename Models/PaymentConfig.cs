using System;

namespace QLS.Backend.Models
{
    public class PaymentConfig
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        // Liên kết với Brand hoặc Store tùy theo chiến lược kinh doanh
        public Guid BrandId { get; set; }
        public Brand Brand { get; set; } = null!;

        public string Provider { get; set; } = "SEPAY"; // Ví dụ: "SEPAY", "MOMO", "VNPAY"
        public bool IsActive { get; set; } = true;

        // Các thông tin dùng cho VietQR / SePay
        public string? BankCode { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }

        // Các thông tin dùng dự phòng nếu tích hợp cổng Gateway khác sau này
        public string? ApiKey { get; set; }
        public string? SecretKey { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
