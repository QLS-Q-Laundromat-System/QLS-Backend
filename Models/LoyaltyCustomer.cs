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

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public bool IsEmailVerified { get; set; }

        public bool IsPhoneNumberVerified { get; set; }

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
