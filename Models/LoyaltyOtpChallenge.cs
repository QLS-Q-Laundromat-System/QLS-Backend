using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLS.Backend.Models
{
    public class LoyaltyOtpChallenge
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid BrandId { get; set; }

        [ForeignKey(nameof(BrandId))]
        public Brand? Brand { get; set; }

        [Required]
        [MaxLength(150)]
        public string Identifier { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string Channel { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Purpose { get; set; } = string.Empty;

        [Required]
        public string CodeHash { get; set; } = string.Empty;

        public int FailedAttempts { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? ConsumedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
