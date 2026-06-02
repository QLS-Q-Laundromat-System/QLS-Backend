using System.ComponentModel.DataAnnotations;

namespace QLS.Backend.DTOs.Loyalty.Auth
{
    public class LoyaltyOtpLoginRequestDto
    {
        [Required]
        public Guid BrandId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Identifier { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^\\d{6}$")]
        public string OtpCode { get; set; } = string.Empty;
    }
}
