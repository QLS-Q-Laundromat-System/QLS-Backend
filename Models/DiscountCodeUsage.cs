using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLS.Backend.Models
{
    public class DiscountCodeUsage
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid DiscountCodeId { get; set; }
        [ForeignKey(nameof(DiscountCodeId))]
        public DiscountCode? DiscountCode { get; set; }

        [Required]
        public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public Guid? MachineSessionId { get; set; }
        [ForeignKey(nameof(MachineSessionId))]
        public MachineSession? MachineSession { get; set; }

        public DateTime UsedAt { get; set; } = DateTime.UtcNow;
    }
}
