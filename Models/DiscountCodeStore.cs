using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLS.Backend.Models
{
    public class DiscountCodeStore
    {
        [Required]
        public Guid DiscountCodeId { get; set; }
        [ForeignKey(nameof(DiscountCodeId))]
        public DiscountCode? DiscountCode { get; set; }

        [Required]
        public Guid StoreId { get; set; }
        [ForeignKey(nameof(StoreId))]
        public Store? Store { get; set; }
    }
}
