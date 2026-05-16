namespace QLS.Backend.DTOs.Loyalty
{
    public class LoyaltyPointHistoryItemDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public int Points { get; set; }
        public int RemainingPoints { get; set; }
        public Guid? MachineSessionId { get; set; }
        public DateTime? ExpiredAt { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
