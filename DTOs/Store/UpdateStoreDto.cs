using System;
using System.ComponentModel.DataAnnotations;

namespace QLS.Backend.DTOs.Store
{
    public class UpdateStoreDto
    {
        [Required(ErrorMessage = "Tên cửa hàng không được để trống")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        public string Address { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;
        
        public string Email { get; set; } = string.Empty;

        public string? StoreId { get; set; }

        public bool IsActive { get; set; }
    }
}
