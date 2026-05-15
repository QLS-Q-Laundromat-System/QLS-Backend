using Microsoft.Extensions.Hosting;
using QLS.Backend.Data;
using QLS.Backend.Models.Enums;
using QLS.Backend.Interfaces;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Models;

namespace QLS.Backend.Services.Machine;

public class MachineStatusMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MachineStatusMonitoringService> _logger;
    private readonly IHardwareTrackerService _hardwareTracker;

    public MachineStatusMonitoringService(IServiceProvider serviceProvider, ILogger<MachineStatusMonitoringService> logger, IHardwareTrackerService hardwareTracker)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _hardwareTracker = hardwareTracker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("✅ Machine Status Monitoring Service đã bắt đầu.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorSessionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi trong quá trình quét trạng thái máy.");
            }

            // Quét mỗi 10 giây
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task MonitorSessionsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var machineDetailService = scope.ServiceProvider.GetRequiredService<IMachineDetailService>();
        var machineService = scope.ServiceProvider.GetRequiredService<IMachineService>();

        // 1. Tìm các Store có ít nhất một session cần quét gấp (Status 5, hoặc Status 1 sắp xong/quá 5p)
        var now = DateTime.UtcNow;
        var storesToScan = await context.MachineSessions
            .Where(s => s.Status == MachineSessionStatus.PaidWaitingForStart || 
                       (s.Status == MachineSessionStatus.Running && 
                        (s.UpdatedAt < now.AddMinutes(-5) || s.EndTime < now.AddMinutes(2))))
            .Select(s => s.Machine.StoreId)
            .Distinct()
            .ToListAsync(stoppingToken);

        if (!storesToScan.Any()) return;

        // 2. Lấy TOÀN BỘ session đang hoạt động (1 hoặc 5) của các Store này để quét đồng bộ
        var sessions = await context.MachineSessions
            .Where(s => (s.Status == MachineSessionStatus.PaidWaitingForStart || s.Status == MachineSessionStatus.Running) &&
                        storesToScan.Contains(s.Machine.StoreId))
            .Include(s => s.Machine)
            .Include(s => s.Store)
            .ToListAsync(stoppingToken);

        if (!sessions.Any()) 
        {
            return;
        }

        foreach(var s in sessions) {
            var remain = (s.EndTime - now).TotalMinutes;
            _logger.LogInformation("🔍 [MONITOR] Đang quét Session {Id} | Status: {Status} | Còn lại: {Remain:F1} phút", 
                s.Id, s.Status, remain);
        }

        // 2. Group theo Store để gọi LG API
        var storeGroups = sessions.GroupBy(s => s.Machine.StoreId);

        foreach (var group in storeGroups)
        {
            var store = group.First().Store;
            if (store == null || string.IsNullOrEmpty(store.StoreId)) continue;

            try
            {
                var machineStatuses = await machineDetailService.GetLgMachineStatusAsync(store.StoreId);
                
                foreach (var session in group)
                {
                    var status = machineStatuses.FirstOrDefault(m => m.DeviceId == session.Machine.LgDeviceId);
                    if (status == null) continue;

                    var curState = status.CurState?.ToUpper();
                    var process = status.Process?.ToUpper();

                    if (session.Machine == null) continue;

                    // --- TRƯỜNG HỢP A: CHỜ KHÁCH BẤM START (Status 5) ---
                    if (session.Status == MachineSessionStatus.PaidWaitingForStart)
                    {
                        // Kiểm tra Timeout 5 phút
                        var elapsed = now - session.UpdatedAt;
                        if (elapsed.TotalMinutes > 5)
                        {
                            _logger.LogWarning("⏰ Session {SessionId} quá 5 phút khách không bấm Start. Chuyển Error.", session.Id);
                            await machineService.UpdateSessionStatusAsync(session.Id, MachineSessionStatus.Error, "Hết thời gian chờ (5p). Khách hàng không nhấn nút START.");
                            continue;
                        }

                        // Kiểm tra nếu khách đã bấm Start (WASHING hoặc DRYING)
                        bool userPressedStart = false;
                        if (session.Machine.Type == MachineType.Washer)
                        {
                            if (curState?.Contains("WASHING") == true || curState == "RUNNING") userPressedStart = true;
                        }
                        else 
                        {
                            if (process?.Contains("DRYING") == true || process == "RUNNING") userPressedStart = true;
                        }

                        if (userPressedStart)
                        {
                            // Bắt lại thông tin Course/CourseNum tùy loại máy
                            session.CycleName = session.Machine.Type == MachineType.Washer ? status.CourseNum : status.Course;

                            _logger.LogInformation("🚀 [START] Khách đã bấm nút. Máy {DeviceId} bắt đầu chạy (Course: {Course}). Session {Id} -> Running.", 
                                status.DeviceId, session.CycleName, session.Id);
                            await machineService.UpdateSessionStatusAsync(session.Id, MachineSessionStatus.Running);
                        }
                        else if (curState == "NOT_SELECTED" || curState == "IDLE")
                        {
                            _logger.LogDebug("⏳ [READY] Máy {DeviceId} đã nhận xu, đang chờ khách chọn chương trình và bấm START.", status.DeviceId);
                        }
                    }

                    // --- TRƯỜNG HỢP B: ĐANG CHẠY - THEO DÕI KẾT THÚC (Status 1) ---
                    else if (session.Status == MachineSessionStatus.Running)
                    {
                        // Cập nhật Course nếu chưa có (phòng trường hợp lúc bắt đầu chưa bắt được)
                        if (string.IsNullOrEmpty(session.CycleName))
                        {
                            session.CycleName = session.Machine.Type == MachineType.Washer ? status.CourseNum : status.Course;
                        }

                        // Kiểm tra nếu máy báo lỗi từ LG
                        if (curState == "ERROR" || curState == "FAULT")
                        {
                            _logger.LogError("❌ [HARDWARE ERROR] Máy {DeviceId} báo lỗi LG: {State}. Session {Id} -> Error.", status.DeviceId, curState, session.Id);
                            await machineService.UpdateSessionStatusAsync(session.Id, MachineSessionStatus.Error, $"Máy báo lỗi phần cứng LG: {curState}");
                            continue;
                        }

                        // Kiểm tra nếu đã đến lúc kết thúc
                        // Nếu thời gian hiện tại vượt quá EndTime hoặc máy quay về trạng thái rảnh
                        bool isFinished = false;
                        
                        // Nếu máy báo NOT_SELECTED hoặc IDLE sau khi đã chạy được ít nhất 1 phút
                        var runningTime = now - session.StartTime;
                        if (runningTime.TotalMinutes > 1 && (curState == "NOT_SELECTED" || curState == "IDLE" || curState == "INITIAL"))
                        {
                            isFinished = true;
                        }
                        
                        // Backup: Nếu quá EndTime 10 phút mà vẫn chưa thấy báo finish (phòng trường hợp mất kết nối)
                        if (now > session.EndTime.AddMinutes(10))
                        {
                            _logger.LogWarning("⚠️ [BACKUP FINISH] Session {Id} quá EndTime 10 phút, tự động kết thúc.", session.Id);
                            isFinished = true;
                        }

                        if (isFinished)
                        {
                            _logger.LogInformation("🏁 [COMPLETED] Máy {DeviceId} đã giặt/sấy xong. Session {Id} -> Completed.", status.DeviceId, session.Id);
                            await machineService.UpdateSessionStatusAsync(session.Id, MachineSessionStatus.Completed);
                        }
                    }
                }

                // SAU KHI QUÉT XONG: Cập nhật lại UpdatedAt cho tất cả session trong group này
                // để lần sau nó không quét lại ngay (đợi 5 phút tiếp theo)
                foreach (var session in group)
                {
                    session.UpdatedAt = DateTime.UtcNow;
                }
                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Lỗi quét trạng thái Store {StoreId}: {Msg}", store.StoreId, ex.Message);
            }
        }
    }
}
