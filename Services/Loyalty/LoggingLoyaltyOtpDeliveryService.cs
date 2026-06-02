using QLS.Backend.Interfaces.Loyalty;
using QLS.Backend.Exceptions;

namespace QLS.Backend.Services.Loyalty
{
    public class LoggingLoyaltyOtpDeliveryService : ILoyaltyOtpDeliveryService
    {
        private readonly ILogger<LoggingLoyaltyOtpDeliveryService> _logger;
        private readonly IConfiguration _configuration;

        public LoggingLoyaltyOtpDeliveryService(
            ILogger<LoggingLoyaltyOtpDeliveryService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public Task SendAsync(string channel, string identifier, string otpCode)
        {
            if (!_configuration.GetValue<bool>("LoyaltyAuth:EnableDevelopmentOtpDelivery"))
            {
                throw new ApiException("Chưa cấu hình nhà cung cấp gửi OTP.", 503);
            }

            _logger.LogWarning("[LOYALTY OTP DEV] {Channel} {Identifier}: {OtpCode}", channel, identifier, otpCode);
            return Task.CompletedTask;
        }
    }
}
