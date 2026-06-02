using System.ComponentModel.DataAnnotations;

namespace QLS.Backend.DTOs.Loyalty.Auth
{
    public class LoyaltyPasswordLoginRequestDto
    {
        [Required]
        public Guid BrandId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Identifier { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
