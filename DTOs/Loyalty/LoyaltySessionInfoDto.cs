namespace QLS.Backend.DTOs.Loyalty
{
    public class LoyaltySessionInfoDto
    {
        public string ClaimQrUrl { get; set; } = string.Empty;
        public DateTime ExpiredAt { get; set; }
        public int PointsToEarn { get; set; }
        public bool IsClaimed { get; set; }
    }
}
