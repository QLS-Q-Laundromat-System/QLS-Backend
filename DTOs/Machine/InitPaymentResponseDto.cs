using System;

namespace QLS.Backend.DTOs.Machine
{
    public class InitPaymentResponseDto
    {
        public Guid SessionId { get; set; }

        public decimal ServerCalculatedAmount { get; set; }
        public int TotalMinutes { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;

        public string? PaymentCode { get; set; }
        public string? QrUrl { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}

