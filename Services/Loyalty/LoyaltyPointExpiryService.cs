using Microsoft.Extensions.Hosting;
using QLS.Backend.Interfaces.Loyalty;

namespace QLS.Backend.Services.Loyalty
{
    public class LoyaltyPointExpiryService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LoyaltyPointExpiryService> _logger;

        public LoyaltyPointExpiryService(IServiceProvider serviceProvider, ILogger<LoyaltyPointExpiryService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("✅ Loyalty Point Expiry Service đã bắt đầu.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var loyaltyService = scope.ServiceProvider.GetRequiredService<ILoyaltyService>();
                    await loyaltyService.ProcessExpiredPointsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Loyalty Point Expiry Service gặp lỗi khi xử lý điểm hết hạn.");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
