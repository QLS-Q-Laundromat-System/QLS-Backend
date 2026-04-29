using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs.Dashboard;
using QLS.Backend.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QLS.Backend.Controllers.Dashboard
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 1. WASH COUNT
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// [Dashboard] Tổng quan số lần giặt/sấy.
        /// Hỗ trợ lọc theo Brand hoặc Store. Dùng cho cả Super Admin lẫn Brand Admin.
        /// </summary>
        /// <param name="brandId">Lọc theo brand (tùy chọn)</param>
        /// <param name="storeId">Lọc theo store (tùy chọn, ưu tiên hơn brandId)</param>
        /// <param name="days">Số ngày hồi cố (mặc định 30 ngày)</param>
        [HttpGet("wash-count/summary")]
        public async Task<ActionResult<WashCountSummaryDto>> GetWashCountSummary(
            [FromQuery] Guid? brandId,
            [FromQuery] Guid? storeId,
            [FromQuery] int days = 30)
        {
            var result = await _dashboardService.GetWashCountSummaryAsync(brandId, storeId, days);
            return Ok(result);
        }

        /// <summary>
        /// [Dashboard] Số lần giặt/sấy theo từng ngày (để vẽ biểu đồ cột/đường).
        /// Trả về mảng daily breakdown, mỗi điểm có WashCount (giặt) và DryCount (sấy).
        /// </summary>
        [HttpGet("wash-count/daily")]
        public async Task<ActionResult<List<DailyWashCountDto>>> GetDailyWashCount(
            [FromQuery] Guid? brandId,
            [FromQuery] Guid? storeId,
            [FromQuery] int days = 30)
        {
            var result = await _dashboardService.GetDailyWashCountAsync(brandId, storeId, days);
            return Ok(result);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 2. TRANSACTION VOLUME
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// [Dashboard] Lưu lượng giao dịch theo thời gian (Real-time trend).
        /// Dùng để phát hiện sự cố nghẽn cổng thanh toán: khi FailureRate tăng đột biến.
        /// 
        /// - granularity=hourly: xem theo giờ (tốt cho 24-48h gần nhất)
        /// - granularity=daily: xem theo ngày (tốt cho 7-30 ngày)
        /// - rangeHours: khoảng thời gian hồi cố tính bằng giờ (VD: 24 = 24 giờ qua)
        /// 
        /// OverallFailureRate trong response cho biết % giao dịch lỗi/hủy tổng thể.
        /// </summary>
        [HttpGet("transaction-volume")]
        public async Task<ActionResult<TransactionVolumeResponseDto>> GetTransactionVolume(
            [FromQuery] Guid? brandId,
            [FromQuery] string granularity = "hourly",
            [FromQuery] int rangeHours = 24)
        {
            if (granularity != "hourly" && granularity != "daily")
                return BadRequest("granularity phải là 'hourly' hoặc 'daily'.");

            if (rangeHours < 1 || rangeHours > 8760)
                return BadRequest("rangeHours phải nằm trong khoảng 1 - 8760 (1 năm).");

            var result = await _dashboardService.GetTransactionVolumeAsync(brandId, granularity, rangeHours);
            return Ok(result);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 3. BRAND GROWTH
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// [Dashboard - Super Admin] Biểu đồ tăng trưởng số lượng Brand/Tenant theo tháng.
        /// Trả về số brand mới từng tháng và tổng tích lũy (cumulative) để vẽ area chart.
        /// </summary>
        /// <param name="months">Số tháng hồi cố (mặc định 12 tháng)</param>
        [HttpGet("brand-growth")]
        public async Task<ActionResult<BrandGrowthResponseDto>> GetBrandGrowth(
            [FromQuery] int months = 12)
        {
            if (months < 1 || months > 60)
                return BadRequest("months phải nằm trong khoảng 1 - 60.");

            var result = await _dashboardService.GetBrandGrowthAsync(months);
            return Ok(result);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 4. SYSTEM OVERVIEW
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// [Dashboard - Super Admin] Tổng quan toàn bộ hệ thống:
        /// Số brand, store, máy, user, session đang chạy, doanh thu hôm nay.
        /// Đây là API cho widget "KPI Cards" ở đầu trang admin dashboard.
        /// </summary>
        [HttpGet("system-overview")]
        public async Task<ActionResult<SystemOverviewDto>> GetSystemOverview()
        {
            var result = await _dashboardService.GetSystemOverviewAsync();
            return Ok(result);
        }

        /// <summary>
        /// [Dashboard - Brand Admin] Tổng quan cho 1 Brand:
        /// Số store, máy, session đang chạy, doanh thu hôm nay của Brand.
        /// </summary>
        [HttpGet("brand-overview/{brandId}")]
        public async Task<ActionResult<BrandOverviewDto>> GetBrandOverview(Guid brandId)
        {
            var result = await _dashboardService.GetBrandOverviewAsync(brandId);
            return Ok(result);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 5. MACHINE UTILIZATION
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet("machine-utilization")]
        public async Task<ActionResult<List<StoreUtilizationDto>>> GetMachineUtilization(
            [FromQuery] Guid? brandId,
            [FromQuery] Guid? storeId,
            [FromQuery] int days = 30)
        {
            var result = await _dashboardService.GetMachineUtilizationAsync(brandId, storeId, days);
            return Ok(result);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 6. PEAK HOURS
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet("peak-hours")]
        public async Task<ActionResult<List<PeakHourDto>>> GetPeakHours(
            [FromQuery] Guid? brandId,
            [FromQuery] Guid? storeId,
            [FromQuery] int days = 30)
        {
            var result = await _dashboardService.GetPeakHoursAsync(brandId, storeId, days);
            return Ok(result);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 7. REVENUE TREND
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet("revenue-trend")]
        public async Task<ActionResult<RevenueTrendResponseDto>> GetRevenueTrend(
            [FromQuery] Guid? brandId,
            [FromQuery] Guid? storeId,
            [FromQuery] int days = 30)
        {
            var result = await _dashboardService.GetRevenueTrendAsync(brandId, storeId, days);
            return Ok(result);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 8. MACHINE STATUS SUMMARY
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet("machine-status-summary")]
        public async Task<ActionResult<MachineStatusSummaryDto>> GetMachineStatusSummary(
            [FromQuery] Guid? brandId,
            [FromQuery] Guid? storeId)
        {
            var result = await _dashboardService.GetMachineStatusSummaryAsync(brandId, storeId);
            return Ok(result);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 9. LEADERBOARD
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet("leaderboard")]
        public async Task<ActionResult<List<LeaderboardEntryDto>>> GetLeaderboard(
            [FromQuery] string type = "brand",
            [FromQuery] int top = 5)
        {
            var result = await _dashboardService.GetLeaderboardAsync(type, top);
            return Ok(result);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 10. USER GROWTH
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet("user-growth")]
        public async Task<ActionResult<List<UserGrowthPointDto>>> GetUserGrowth(
            [FromQuery] int months = 12)
        {
            var result = await _dashboardService.GetUserGrowthAsync(months);
            return Ok(result);
        }
    }
}
