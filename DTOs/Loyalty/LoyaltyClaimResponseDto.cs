namespace QLS.Backend.DTOs.Loyalty
{
    public class LoyaltyClaimResponseDto
    {
        public Guid CustomerId { get; set; }
        public Guid MachineSessionId { get; set; }
        public int ClaimedPoints { get; set; }
        public int TotalPoints { get; set; }
        public DateTime ClaimedAt { get; set; }
    }
}
