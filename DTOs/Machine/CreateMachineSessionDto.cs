using QLS.Backend.Models.Enums;
using System;

namespace QLS.Backend.DTOs.Machine
{
    public class CreateMachineSessionDto
    {
        public Guid BranchId { get; set; }
        public Guid MachineId { get; set; }
        public Guid UserId { get; set; }
        public int TotalMinutes { get; set; }
        public decimal PricePaid { get; set; }
        public Guid? PriceListId { get; set; }
        public PricePerType PricingMode { get; set; } = PricePerType.Flat;
        public decimal? WeightKg { get; set; }
        public string? CycleName { get; set; }
        public bool IsExtension { get; set; } = false;
    }
}
