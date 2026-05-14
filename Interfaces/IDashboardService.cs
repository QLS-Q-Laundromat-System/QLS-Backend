using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QLS.Backend.DTOs.Dashboard;

namespace QLS.Backend.Interfaces
{
    public interface IDashboardService
    {
        // 1. Số lần giặt/sấy (theo hệ thống hoặc brand)
        Task<WashCountSummaryDto> GetWashCountSummaryAsync(Guid? brandId, Guid? storeId, int days);
        Task<List<DailyWashCountDto>> GetDailyWashCountAsync(Guid? brandId, Guid? storeId, int days);

        // 2. Lưu lượng giao dịch theo thời gian
        Task<TransactionVolumeResponseDto> GetTransactionVolumeAsync(Guid? brandId, string granularity, int rangeHours);

        // 3. Tăng trưởng Brand
        Task<BrandGrowthResponseDto> GetBrandGrowthAsync(int months);

        // 4. Tổng quan hệ thống (Super Admin)
        Task<SystemOverviewDto> GetSystemOverviewAsync();

        // 4.1. Tổng quan Brand (Brand Admin)
        Task<BrandOverviewDto> GetBrandOverviewAsync(Guid brandId);

        // 5. Tỷ lệ sử dụng máy
        Task<List<StoreUtilizationDto>> GetMachineUtilizationAsync(Guid? brandId, Guid? storeId, int days);

        // 6. Khung giờ cao điểm
        Task<List<PeakHourDto>> GetPeakHoursAsync(Guid? brandId, Guid? storeId, int days);

        // 7. Doanh thu theo thời gian
        Task<RevenueTrendResponseDto> GetRevenueTrendAsync(Guid? brandId, Guid? storeId, int days);

        // 8. Trạng thái máy hiện tại (Live Status)
        Task<MachineStatusSummaryDto> GetMachineStatusSummaryAsync(Guid? brandId, Guid? storeId);

        // 9. Bảng xếp hạng (Leaderboard)
        Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(string type, int top = 5);

        // 10. Tăng trưởng người dùng
        Task<List<UserGrowthPointDto>> GetUserGrowthAsync(int months);
    }
}
