using System;
using System.ComponentModel.DataAnnotations;

namespace QLS.Backend.DTOs.Store
{
    public class CreateStoreDto
    {
        [Required(ErrorMessage = "Tên cửa hàng không được để trống")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        public string Address { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? StoreId { get; set; }

        // LG ThinQ Integration Fields
        public string? City { get; set; }
        public string? Zipcode { get; set; }
        public string? States { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public string LTime { get; set; } = "Asia/Saigon";

        [Required(ErrorMessage = "BrandId không được để trống")]
        public Guid BrandId { get; set; }

        public Guid? StoreTypeId { get; set; }
    }
}
