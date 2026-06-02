namespace QLS.Backend.DTOs.Loyalty.Auth
{
    public class LoyaltyOtpRequestResponseDto
    {
        public DateTime ExpiresAt { get; set; }
        public string Channel { get; set; } = string.Empty;
        public string? DevelopmentOtpCode { get; set; }
    }
}
