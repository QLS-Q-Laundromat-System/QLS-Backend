using System;
using System.ComponentModel.DataAnnotations;

namespace QLS.Backend.DTOs.Brand
{
    public class UpdateBrandDto
    {
        [Required(ErrorMessage = "Tên chuỗi không được để trống")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email liên hệ không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public string? Logo { get; set; }

        public bool IsActive { get; set; }
    }
}
