using System;
using System.ComponentModel.DataAnnotations;

namespace QLS.Backend.DTOs.Brand
{
    // DTO dùng để hứng dữ liệu khi SuperAdmin tạo chủ chuỗi mới
    public class CreateBrandDto
    {
        [Required(ErrorMessage = "Tên chuỗi không được để trống")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email liên hệ không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        public string? ContactPhone { get; set; }
    }

    // DTO dùng để trả dữ liệu danh sách chủ chuỗi ra cho Frontend (giấu đi các thông tin nhạy cảm)
    public class BrandResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
