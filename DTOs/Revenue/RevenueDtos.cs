using System;
using System.Collections.Generic;

namespace QLS.Backend.DTOs.Revenue
{
    public class RevenueSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalSessions { get; set; }
        public decimal AverageRevenuePerSession => TotalSessions > 0 ? TotalRevenue / TotalSessions : 0;
    }

    public class DailyRevenueDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int SessionCount { get; set; }
    }

    public class StoreRevenueDto
    {
        public Guid StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int SessionCount { get; set; }
    }

    public class MachineRevenueRankingDto
    {
        public Guid MachineId { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int SessionCount { get; set; }
    }
}
