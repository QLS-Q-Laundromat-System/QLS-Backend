using System;

namespace QLS.Backend.DTOs.Brand
{
    // DTO dùng để trả dữ liệu danh sách chủ chuỗi ra cho Frontend (giấu đi các thông tin nhạy cảm)
    public class BrandResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public string? Logo { get; set; }
        public int StoreCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
