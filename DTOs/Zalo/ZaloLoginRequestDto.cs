using System.ComponentModel.DataAnnotations;

namespace QLS.Backend.DTOs.Zalo
{
    public class ZaloLoginRequestDto
    {
        [Required]
        public Guid BrandId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ZaloUserId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ZaloOAUserId { get; set; }

        [MaxLength(150)]
        public string? FullName { get; set; }

        [MaxLength(500)]
        public string? AvatarUrl { get; set; }
    }
}
