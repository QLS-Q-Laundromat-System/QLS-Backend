using System;
using System.Collections.Generic;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.DTOs.Dashboard
{
    // ─────────────────────────────────────────────────────────────────────────────
    // 1. WASH COUNT - Số lần giặt/sấy
    // ─────────────────────────────────────────────────────────────────────────────

    public class WashCountSummaryDto
    {
        /// <summary>Tổng số lần giặt (Washer)</summary>
        public int TotalWashSessions { get; set; }

        /// <summary>Tổng số lần sấy (Dryer)</summary>
        public int TotalDrySessions { get; set; }

        /// <summary>Tổng cộng (giặt + sấy)</summary>
        public int TotalSessions => TotalWashSessions + TotalDrySessions;

        /// <summary>Khoảng thời gian thống kê (ngày)</summary>
        public int DaysRange { get; set; }
    }

    public class DailyWashCountDto
    {
        public DateTime Date { get; set; }
        public int WashCount { get; set; }
        public int DryCount { get; set; }
        public int TotalCount => WashCount + DryCount;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 2. TRANSACTION VOLUME - Lưu lượng giao dịch
    // ─────────────────────────────────────────────────────────────────────────────

    public class TransactionVolumePointDto
    {
        /// <summary>Mốc thời gian (giờ hoặc ngày tùy granularity)</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Số giao dịch thành công (Completed)</summary>
        public int Completed { get; set; }

        /// <summary>Số giao dịch đang chạy (Running)</summary>
        public int Running { get; set; }

        /// <summary>Số giao dịch lỗi/hủy (Error + Cancelled)</summary>
        public int Failed { get; set; }

        /// <summary>Tổng giao dịch</summary>
        public int Total => Completed + Running + Failed;

        /// <summary>Tỷ lệ thành công %</summary>
        public double SuccessRate => Total > 0 ? Math.Round((double)Completed / Total * 100, 1) : 0;
    }

    public class TransactionVolumeResponseDto
    {
        /// <summary>Danh sách các điểm dữ liệu trên timeline</summary>
        public List<TransactionVolumePointDto> DataPoints { get; set; } = new();

        /// <summary>Granularity: "hourly" hoặc "daily"</summary>
        public string Granularity { get; set; } = "hourly";

        /// <summary>Tổng giao dịch toàn bộ khoảng thời gian</summary>
        public int TotalCompleted { get; set; }
        public int TotalFailed { get; set; }

        /// <summary>Tỷ lệ lỗi tổng thể % - dùng để cảnh báo nghẽn cổng thanh toán</summary>
        public double OverallFailureRate =>
            (TotalCompleted + TotalFailed) > 0
                ? Math.Round((double)TotalFailed / (TotalCompleted + TotalFailed) * 100, 1)
                : 0;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 3. BRAND GROWTH - Tăng trưởng số Brand
    // ─────────────────────────────────────────────────────────────────────────────

    public class BrandGrowthPointDto
    {
        /// <summary>Tháng/Năm (VD: 2025-01)</summary>
        public string Month { get; set; } = string.Empty;

        /// <summary>Số brand mới đăng ký trong tháng này</summary>
        public int NewBrands { get; set; }

        /// <summary>Tổng brand tích lũy đến tháng này (cumulative)</summary>
        public int CumulativeTotal { get; set; }

        /// <summary>Số brand đang active tích lũy đến tháng này</summary>
        public int ActiveTotal { get; set; }
    }

    public class BrandGrowthResponseDto
    {
        public List<BrandGrowthPointDto> DataPoints { get; set; } = new();

        /// <summary>Tổng brand hiện tại</summary>
        public int TotalBrands { get; set; }

        /// <summary>Brand đang active</summary>
        public int ActiveBrands { get; set; }

        /// <summary>Brand inactive/bị đình chỉ</summary>
        public int InactiveBrands { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 4. SYSTEM OVERVIEW - Tổng quan hệ thống (cho Super Admin)
    // ─────────────────────────────────────────────────────────────────────────────

    public class SystemOverviewDto
    {
        public int TotalBrands { get; set; }
        public int ActiveBrands { get; set; }

        public int TotalStores { get; set; }
        public int ActiveStores { get; set; }

        public int TotalMachines { get; set; }
        public int WasherCount { get; set; }
        public int DryerCount { get; set; }

        public int TotalUsers { get; set; }

        /// <summary>Số session đang chạy ngay lúc này</summary>
        public int ActiveSessionsNow { get; set; }

        /// <summary>Tổng doanh thu hôm nay</summary>
        public decimal TodayRevenue { get; set; }

        /// <summary>Số lần giặt hôm nay</summary>
        public int TodaySessionCount { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 5. MACHINE UTILIZATION - Tỷ lệ sử dụng máy
    // ─────────────────────────────────────────────────────────────────────────────

    public class MachineUtilizationDto
    {
        public Guid MachineId { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public MachineType MachineType { get; set; }

        /// <summary>Tổng số lần được sử dụng</summary>
        public int SessionCount { get; set; }

        /// <summary>Tổng số phút máy chạy</summary>
        public int TotalMinutesRunning { get; set; }

        /// <summary>Doanh thu từ máy này</summary>
        public decimal Revenue { get; set; }
    }

    public class StoreUtilizationDto
    {
        public Guid StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public List<MachineUtilizationDto> Machines { get; set; } = new();

        /// <summary>Tổng số session của cả store</summary>
        public int TotalSessions { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 6. PEAK HOURS - Khung giờ cao điểm
    // ─────────────────────────────────────────────────────────────────────────────

    public class PeakHourDto
    {
        /// <summary>Giờ trong ngày (0-23)</summary>
        public int Hour { get; set; }

        /// <summary>Nhãn hiển thị (VD: "08:00 - 09:00")</summary>
        public string Label => $"{Hour:D2}:00 - {(Hour + 1) % 24:D2}:00";

        /// <summary>Số session trong khung giờ này</summary>
        public int SessionCount { get; set; }

        /// <summary>Doanh thu trung bình mỗi giờ này</summary>
        public decimal AvgRevenue { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 7. REVENUE TREND - Phân tích doanh thu theo thời gian
    // ─────────────────────────────────────────────────────────────────────────────

    public class RevenueTrendPointDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int SessionCount { get; set; }
    }

    public class RevenueTrendResponseDto
    {
        public List<RevenueTrendPointDto> DataPoints { get; set; } = new();
        public decimal TotalRevenue { get; set; }
        public double GrowthRate { get; set; } // So với kỳ trước (%)
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 8. MACHINE STATUS - Tổng quan trạng thái máy hiện tại
    // ─────────────────────────────────────────────────────────────────────────────

    public class MachineStatusSummaryDto
    {
        public int Total { get; set; }
        public int Ready { get; set; }
        public int Running { get; set; }
        public int Error { get; set; }
        public int Offline { get; set; }
        public int Maintenance { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 9. LEADERBOARD - Bảng xếp hạng doanh thu
    // ─────────────────────────────────────────────────────────────────────────────

    public class LeaderboardEntryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; } // Doanh thu hoặc số session
        public int Rank { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 10. USER GROWTH - Tăng trưởng thành viên
    // ─────────────────────────────────────────────────────────────────────────────

    public class UserGrowthPointDto
    {
        public string Month { get; set; } = string.Empty;
        public int NewUsers { get; set; }
        public int TotalUsers { get; set; }
    }
}
