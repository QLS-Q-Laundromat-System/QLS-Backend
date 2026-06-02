namespace QLS.Backend.Interfaces.Loyalty
{
    public interface ILoyaltyOtpDeliveryService
    {
        Task SendAsync(string channel, string identifier, string otpCode);
    }
}
