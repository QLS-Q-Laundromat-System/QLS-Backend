using System;

namespace QLS.Backend.DTOs.Machine
{
    public class InitPaymentRequestDto
    {
        public Guid StoreId { get; set; }
        public Guid MachineId { get; set; }
        public Guid UserId { get; set; }
        public string PaymentMethod { get; set; } = "QR";
        public int? RequestedSteps { get; set; } 
        public decimal? WeightKg { get; set; } 
    }
}
