using System;

namespace QLS.Backend.DTOs.Store
{
    public class StoreResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? StoreId { get; set; }
        public string? LgPinCode { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid BrandId { get; set; }
    }
}
