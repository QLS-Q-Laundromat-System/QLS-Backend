using System;

namespace QLS.Backend.DTOs.Machine
{
    public class InitPaymentResponseDto
    {
        public decimal ServerCalculatedAmount { get; set; }
        public int TotalMinutes { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
