using System;

namespace QLS.Backend.DTOs.Brand
{
    public class CreatePaymentConfigDto
    {
        public Guid BrandId { get; set; }
        public string Provider { get; set; } = "SEPAY";
        public string? BankCode { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
        public string? ApiKey { get; set; }
        public string? SecretKey { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdatePaymentConfigDto
    {
        public string? Provider { get; set; }
        public string? BankCode { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
        public string? ApiKey { get; set; }
        public string? SecretKey { get; set; }
        public bool IsActive { get; set; }
    }

    public class PaymentConfigResponseDto
    {
        public Guid Id { get; set; }
        public Guid BrandId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? BankCode { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
        public string? ApiKey { get; set; }
        public string? SecretKey { get; set; }
        public bool IsActive { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
