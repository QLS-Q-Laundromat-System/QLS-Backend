using System.ComponentModel.DataAnnotations;

namespace QLS.Backend.DTOs.Loyalty
{
    public class LoyaltyClaimRequestDto
    {
        [Required]
        [MaxLength(120)]
        public string ClaimToken { get; set; } = string.Empty;
    }
}
