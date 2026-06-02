using System.ComponentModel.DataAnnotations;

namespace QLS.Backend.DTOs.Loyalty.Auth
{
    public class LoyaltyOtpRequestDto
    {
        [Required]
        public Guid BrandId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Identifier { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Register|Login)$")]
        public string Purpose { get; set; } = string.Empty;
    }
}
