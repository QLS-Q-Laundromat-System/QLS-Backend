namespace QLS.Backend.DTOs.Loyalty.Auth
{
    public class LoyaltyAuthResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public string CustomerType { get; set; } = string.Empty;
        public string StudentVerificationStatus { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
    }
}
