using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QLS.Backend.Models
{
    [Index(nameof(Token), IsUnique = true)]
    public class PointClaimToken
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(120)]
        public string Token { get; set; } = string.Empty;

        [Required]
        public Guid BrandId { get; set; }

        [ForeignKey(nameof(BrandId))]
        public Brand? Brand { get; set; }

        [Required]
        public Guid MachineSessionId { get; set; }

        [ForeignKey(nameof(MachineSessionId))]
        public MachineSession? MachineSession { get; set; }

        public Guid? PaymentTransactionId { get; set; }

        [ForeignKey(nameof(PaymentTransactionId))]
        public PaymentTransaction? PaymentTransaction { get; set; }

        [Precision(18, 2)]
        public decimal PaidAmount { get; set; }

        public int PointsToEarn { get; set; }

        public bool IsClaimed { get; set; } = false;

        public DateTime ExpiredAt { get; set; }

        public Guid? ClaimedByCustomerId { get; set; }

        [ForeignKey(nameof(ClaimedByCustomerId))]
        public LoyaltyCustomer? ClaimedByCustomer { get; set; }

        public DateTime? ClaimedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
