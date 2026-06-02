namespace QLS.Backend.DTOs.Loyalty
{
    public class LoyaltyMeResponseDto
    {
        public Guid CustomerId { get; set; }
        public Guid BrandId { get; set; }
        public string ZaloUserId { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string CustomerType { get; set; } = string.Empty;
        public string StudentVerificationStatus { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
    }
}
