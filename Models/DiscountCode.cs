using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QLS.Backend.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace QLS.Backend.Models
{
    [Index(nameof(Code), nameof(BrandId), IsUnique = true)]
    public class DiscountCode
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid BrandId { get; set; }
        [ForeignKey(nameof(BrandId))]
        public Brand? Brand { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        public DiscountType DiscountType { get; set; }

        [Precision(18, 2)]
        public decimal DiscountValue { get; set; }

        [Precision(18, 2)]
        public decimal? MaxDiscountAmount { get; set; }

        [Precision(18, 2)]
        public decimal? MinOrderValue { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; } = 0;

        public int? UserUsageLimit { get; set; }

        public bool IsActive { get; set; } = true;
        
        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsApplyAllStores { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<DiscountCodeStore> DiscountCodeStores { get; set; } = new List<DiscountCodeStore>();
        public ICollection<DiscountCodeUsage> DiscountCodeUsages { get; set; } = new List<DiscountCodeUsage>();
    }
}
