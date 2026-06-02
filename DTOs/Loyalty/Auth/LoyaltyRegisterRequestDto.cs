using System.ComponentModel.DataAnnotations;

namespace QLS.Backend.DTOs.Loyalty.Auth
{
    public class LoyaltyRegisterRequestDto
    {
        [Required]
        public Guid BrandId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Identifier { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^\\d{6}$")]
        public string OtpCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;
    }
}
