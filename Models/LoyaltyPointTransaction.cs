using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Models
{
    public class LoyaltyPointTransaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid BrandId { get; set; }

        [ForeignKey(nameof(BrandId))]
        public Brand? Brand { get; set; }

        [Required]
        public Guid CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public LoyaltyCustomer? Customer { get; set; }

        public Guid? MachineSessionId { get; set; }

        [ForeignKey(nameof(MachineSessionId))]
        public MachineSession? MachineSession { get; set; }

        public PointTransactionType Type { get; set; }

        public int Points { get; set; }

        public int RemainingPoints { get; set; }

        public DateTime? ExpiredAt { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
