using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.DTOs.DiscountCode
{
    public class DiscountCodeCreateDto
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        public DiscountType DiscountType { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal DiscountValue { get; set; }

        public decimal? MaxDiscountAmount { get; set; }

        public decimal? MinOrderValue { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int? UsageLimit { get; set; }

        public int? UserUsageLimit { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsApplyAllStores { get; set; } = true;

        public List<Guid>? StoreIds { get; set; }
    }

    public class DiscountCodeUpdateDto
    {
        public DiscountType? DiscountType { get; set; }
        public decimal? DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinOrderValue { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? UsageLimit { get; set; }
        public int? UserUsageLimit { get; set; }
        public bool? IsActive { get; set; }
        public string? Description { get; set; }
        
        public bool? IsApplyAllStores { get; set; }
        public List<Guid>? StoreIds { get; set; }
    }

    public class DiscountCodeResponseDto
    {
        public Guid Id { get; set; }
        public Guid BrandId { get; set; }
        public string Code { get; set; } = string.Empty;
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinOrderValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public int? UserUsageLimit { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
        public bool IsApplyAllStores { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public List<Guid> StoreIds { get; set; } = new List<Guid>();
    }

    public class ValidateDiscountRequestDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;
        [Required]
        public Guid StoreId { get; set; }
        [Required]
        public decimal OrderTotal { get; set; }
    }

    public class ValidateDiscountResponseDto
    {
        public bool IsValid { get; set; }
        public string? Message { get; set; }
        public decimal DiscountAmount { get; set; }
        public Guid? DiscountCodeId { get; set; }
    }

    public class DiscountCodeOverviewDto
    {
        public int TotalActiveCodes { get; set; }
        public int TotalUsagesThisMonth { get; set; }
        public decimal TotalDiscountAmountThisMonth { get; set; }
        public decimal TotalRevenueFromDiscountedOrdersThisMonth { get; set; }
    }

    public class DiscountCodeUsageDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserPhoneOrEmail { get; set; } = string.Empty;
        public Guid? MachineSessionId { get; set; }
        public DateTime UsedAt { get; set; }
    }
}
