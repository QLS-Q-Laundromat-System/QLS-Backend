using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Models
{
    public class LoyaltyCustomer
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid BrandId { get; set; }

        [ForeignKey(nameof(BrandId))]
        public Brand? Brand { get; set; }

        [Required]
        [MaxLength(100)]
        public string ZaloUserId { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? FullName { get; set; }

        [MaxLength(500)]
        public string? AvatarUrl { get; set; }

        public int TotalPoints { get; set; } = 0;

        public CustomerType CustomerType { get; set; } = CustomerType.Member;

        public StudentVerificationStatus StudentVerificationStatus { get; set; } = StudentVerificationStatus.None;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<LoyaltyPointTransaction> PointTransactions { get; set; } = new List<LoyaltyPointTransaction>();
    }
}
