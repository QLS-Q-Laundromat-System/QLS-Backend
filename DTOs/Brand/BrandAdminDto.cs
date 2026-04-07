using System;

namespace QLS.Backend.DTOs.Brand
{
    public class BrandAdminDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public Guid? BrandId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
