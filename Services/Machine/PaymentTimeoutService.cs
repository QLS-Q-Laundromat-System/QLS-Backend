using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Services.Machine;

/// <summary>
/// Tự hủy các session chưa thanh toán sau thời gian cấu hình.
/// Chạy độc lập với MachineStatusMonitoringService để timeout thanh toán
/// vẫn hoạt động khi tạm tắt việc đồng bộ trạng thái máy.
/// </summary>
public sealed class PaymentTimeoutService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentTimeoutService> _logger;

    public PaymentTimeoutService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<PaymentTimeoutService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timeoutMinutes = _configuration.GetValue("Payment:PendingTimeoutMinutes", 10);
        _logger.LogInformation(
            "💳 Payment Timeout Service đã bắt đầu | Timeout={TimeoutMinutes} phút",
            timeoutMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpirePendingSessionsAsync(timeoutMinutes, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi tự hủy session quá hạn thanh toán.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ExpirePendingSessionsAsync(int timeoutMinutes, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-timeoutMinutes);

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var expiredSessions = await context.MachineSessions
            .Where(session =>
                session.Status == MachineSessionStatus.PendingPayment &&
                session.CreatedAt <= cutoff)
            .ToListAsync(cancellationToken);

        if (expiredSessions.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var session in expiredSessions)
        {
            session.Status = MachineSessionStatus.Cancelled;
            session.ActualEndTime = now;
            session.UpdatedAt = now;

            _logger.LogWarning(
                "⏰ [PAYMENT] Tự hủy session hết hạn | SessionId={SessionId} | PaymentCode={PaymentCode} | CreatedAt={CreatedAt} | TimeoutMinutes={TimeoutMinutes}",
                session.Id,
                session.PaymentCode ?? "<none>",
                session.CreatedAt,
                timeoutMinutes);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
