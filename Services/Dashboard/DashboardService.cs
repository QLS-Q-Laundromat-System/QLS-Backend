using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.DTOs.Dashboard;
using QLS.Backend.Interfaces;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Services.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 1. WASH COUNT - Số lần giặt/sấy
        // ─────────────────────────────────────────────────────────────────────

        public async Task<WashCountSummaryDto> GetWashCountSummaryAsync(Guid? brandId, Guid? storeId, int days)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            var query = _context.MachineSessions
                .Where(s => s.StartTime >= startDate);

            query = ApplyBrandStoreFilter(query, brandId, storeId);

            // JOIN với Machines để lấy MachineType
            var counts = await query
                .Join(_context.Machines, s => s.MachineId, m => m.Id,
                    (s, m) => new { m.Type })
                .GroupBy(x => x.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            return new WashCountSummaryDto
            {
                TotalWashSessions = counts.FirstOrDefault(c => c.Type == MachineType.Washer)?.Count ?? 0,
                TotalDrySessions = counts.FirstOrDefault(c => c.Type == MachineType.Dryer)?.Count ?? 0,
                DaysRange = days
            };
        }

        public async Task<List<DailyWashCountDto>> GetDailyWashCountAsync(Guid? brandId, Guid? storeId, int days)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);

            var query = _context.MachineSessions
                .Where(s => s.StartTime >= startDate);

            query = ApplyBrandStoreFilter(query, brandId, storeId);

            // JOIN với bảng Machines để biết loại máy
            var data = await query
                .Join(_context.Machines, s => s.MachineId, m => m.Id,
                    (s, m) => new { s.StartTime, m.Type })
                .ToListAsync();

            // Group phía .NET (EF không thể group DateOnly + enum cùng lúc hiệu quả trên Npgsql)
            var result = data
                .GroupBy(x => x.StartTime.Date)
                .Select(g => new DailyWashCountDto
                {
                    Date = g.Key,
                    WashCount = g.Count(x => x.Type == MachineType.Washer),
                    DryCount = g.Count(x => x.Type == MachineType.Dryer)
                })
                .OrderBy(r => r.Date)
                .ToList();

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 2. TRANSACTION VOLUME - Lưu lượng giao dịch
        //    Mục đích: phát hiện nghẽn cổng thanh toán theo xu hướng thời gian
        // ─────────────────────────────────────────────────────────────────────

        public async Task<TransactionVolumeResponseDto> GetTransactionVolumeAsync(
            Guid? brandId, string granularity, int rangeHours)
        {
            var startTime = DateTime.UtcNow.AddHours(-rangeHours);

            var query = _context.MachineSessions
                .Where(s => s.CreatedAt >= startTime);

            if (brandId.HasValue)
                query = query.Where(s => s.Store!.BrandId == brandId.Value);

            // Lấy data thô (CreatedAt + Status)
            var raw = await query
                .Select(s => new { s.CreatedAt, s.Status })
                .ToListAsync();

            // Group theo giờ hoặc ngày
            IEnumerable<TransactionVolumePointDto> points;

            if (granularity == "daily")
            {
                points = raw
                    .GroupBy(x => x.CreatedAt.Date)
                    .Select(g => new TransactionVolumePointDto
                    {
                        Timestamp = g.Key,
                        Completed = g.Count(x => x.Status == MachineSessionStatus.Completed),
                        Running = g.Count(x => x.Status == MachineSessionStatus.Running),
                        Failed = g.Count(x => x.Status == MachineSessionStatus.Error
                                           || x.Status == MachineSessionStatus.Cancelled)
                    })
                    .OrderBy(p => p.Timestamp);
            }
            else
            {
                // Mặc định: theo giờ (hourly) - lý tưởng để phát hiện spike
                points = raw
                    .GroupBy(x => new DateTime(x.CreatedAt.Year, x.CreatedAt.Month,
                                               x.CreatedAt.Day, x.CreatedAt.Hour, 0, 0))
                    .Select(g => new TransactionVolumePointDto
                    {
                        Timestamp = g.Key,
                        Completed = g.Count(x => x.Status == MachineSessionStatus.Completed),
                        Running = g.Count(x => x.Status == MachineSessionStatus.Running),
                        Failed = g.Count(x => x.Status == MachineSessionStatus.Error
                                           || x.Status == MachineSessionStatus.Cancelled)
                    })
                    .OrderBy(p => p.Timestamp);
            }

            var pointList = points.ToList();

            return new TransactionVolumeResponseDto
            {
                DataPoints = pointList,
                Granularity = granularity,
                TotalCompleted = pointList.Sum(p => p.Completed),
                TotalFailed = pointList.Sum(p => p.Failed)
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // 3. BRAND GROWTH - Tăng trưởng số Brand theo tháng
        // ─────────────────────────────────────────────────────────────────────

        public async Task<BrandGrowthResponseDto> GetBrandGrowthAsync(int months)
        {
            var brands = await _context.Brands
                .Select(b => new { b.CreatedAt, b.IsActive })
                .ToListAsync();

            var startMonth = DateTime.UtcNow.AddMonths(-months);

            // Tính số brand mới từng tháng
            var monthlyGroups = brands
                .Where(b => b.CreatedAt >= startMonth)
                .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .ToList();

            // Tính tổng tích lũy (cumulative) - kể cả brand trước khoảng thời gian lọc
            var cumulative = new List<BrandGrowthPointDto>();
            int totalSoFar = brands.Count(b => b.CreatedAt < new DateTime(startMonth.Year, startMonth.Month, 1));
            int activeSoFar = brands.Count(b => b.CreatedAt < new DateTime(startMonth.Year, startMonth.Month, 1) && b.IsActive);

            foreach (var group in monthlyGroups)
            {
                int newCount = group.Count();
                int newActive = group.Count(b => b.IsActive);
                totalSoFar += newCount;
                activeSoFar += newActive;

                cumulative.Add(new BrandGrowthPointDto
                {
                    Month = $"{group.Key.Year}-{group.Key.Month:D2}",
                    NewBrands = newCount,
                    CumulativeTotal = totalSoFar,
                    ActiveTotal = activeSoFar
                });
            }

            return new BrandGrowthResponseDto
            {
                DataPoints = cumulative,
                TotalBrands = brands.Count,
                ActiveBrands = brands.Count(b => b.IsActive),
                InactiveBrands = brands.Count(b => !b.IsActive)
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // 4. SYSTEM OVERVIEW - Tổng quan toàn hệ thống (Super Admin)
        // ─────────────────────────────────────────────────────────────────────

        public async Task<SystemOverviewDto> GetSystemOverviewAsync()
        {
            var today = DateTime.UtcNow.Date;

            var brandStats = await _context.Brands
                .GroupBy(b => b.IsActive)
                .Select(g => new { IsActive = g.Key, Count = g.Count() })
                .ToListAsync();

            var storeStats = await _context.Stores
                .GroupBy(s => s.IsActive)
                .Select(g => new { IsActive = g.Key, Count = g.Count() })
                .ToListAsync();

            var machineStats = await _context.Machines
                .GroupBy(m => m.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            var userCount = await _context.Users.CountAsync();

            var activeSessionsNow = await _context.MachineSessions
                .CountAsync(s => s.Status == MachineSessionStatus.Running);

            var todaySessions = await _context.MachineSessions
                .Where(s => s.StartTime >= today &&
                            (s.Status == MachineSessionStatus.Completed || s.Status == MachineSessionStatus.Running))
                .Select(s => new { s.PricePaid })
                .ToListAsync();

            return new SystemOverviewDto
            {
                TotalBrands = brandStats.Sum(b => b.Count),
                ActiveBrands = brandStats.FirstOrDefault(b => b.IsActive)?.Count ?? 0,
                TotalStores = storeStats.Sum(s => s.Count),
                ActiveStores = storeStats.FirstOrDefault(s => s.IsActive)?.Count ?? 0,
                TotalMachines = machineStats.Sum(m => m.Count),
                WasherCount = machineStats.FirstOrDefault(m => m.Type == MachineType.Washer)?.Count ?? 0,
                DryerCount = machineStats.FirstOrDefault(m => m.Type == MachineType.Dryer)?.Count ?? 0,
                TotalUsers = userCount,
                ActiveSessionsNow = activeSessionsNow,
                TodayRevenue = todaySessions.Sum(s => s.PricePaid),
                TodaySessionCount = todaySessions.Count
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // 4.1. BRAND OVERVIEW - Tổng quan cho Brand Admin
        // ─────────────────────────────────────────────────────────────────────

        public async Task<BrandOverviewDto> GetBrandOverviewAsync(Guid brandId)
        {
            var today = DateTime.UtcNow.Date;

            var storeStats = await _context.Stores
                .Where(s => s.BrandId == brandId)
                .GroupBy(s => s.IsActive)
                .Select(g => new { IsActive = g.Key, Count = g.Count() })
                .ToListAsync();

            var machineStats = await _context.Machines
                .Join(_context.Stores, m => m.StoreId, s => s.Id, (m, s) => new { m, s })
                .Where(x => x.s.BrandId == brandId)
                .GroupBy(x => x.m.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            var activeSessionsNow = await _context.MachineSessions
                .CountAsync(s => s.Store!.BrandId == brandId && s.Status == MachineSessionStatus.Running);

            var todaySessions = await _context.MachineSessions
                .Where(s => s.Store!.BrandId == brandId && s.StartTime >= today &&
                            (s.Status == MachineSessionStatus.Completed || s.Status == MachineSessionStatus.Running))
                .Select(s => new { s.PricePaid })
                .ToListAsync();

            return new BrandOverviewDto
            {
                TotalStores = storeStats.Sum(s => s.Count),
                ActiveStores = storeStats.FirstOrDefault(s => s.IsActive)?.Count ?? 0,
                TotalMachines = machineStats.Sum(m => m.Count),
                WasherCount = machineStats.FirstOrDefault(m => m.Type == MachineType.Washer)?.Count ?? 0,
                DryerCount = machineStats.FirstOrDefault(m => m.Type == MachineType.Dryer)?.Count ?? 0,
                ActiveSessionsNow = activeSessionsNow,
                TodayRevenue = todaySessions.Sum(s => s.PricePaid),
                TodaySessionCount = todaySessions.Count
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // 5. MACHINE UTILIZATION - Tỷ lệ & hiệu suất sử dụng từng máy
        //    Dùng để phát hiện máy quá tải hoặc máy không được dùng
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<StoreUtilizationDto>> GetMachineUtilizationAsync(
            Guid? brandId, Guid? storeId, int days)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            var query = _context.MachineSessions
                .Where(s => s.StartTime >= startDate);

            query = ApplyBrandStoreFilter(query, brandId, storeId);

            var data = await query
                .Join(_context.Machines, s => s.MachineId, m => m.Id,
                    (s, m) => new { s.StoreId, s.MachineId, MachineName = m.Name, m.Type, s.TotalMinutes, s.PricePaid })
                .Join(_context.Stores, x => x.StoreId, st => st.Id,
                    (x, st) => new { x.StoreId, StoreName = st.Name, x.MachineId, x.MachineName, x.Type, x.TotalMinutes, x.PricePaid })
                .ToListAsync();

            var result = data
                .GroupBy(x => new { x.StoreId, x.StoreName })
                .Select(storeGroup => new StoreUtilizationDto
                {
                    StoreId = storeGroup.Key.StoreId,
                    StoreName = storeGroup.Key.StoreName,
                    TotalSessions = storeGroup.Count(),
                    Machines = storeGroup
                        .GroupBy(x => new { x.MachineId, x.MachineName, x.Type })
                        .Select(machineGroup => new MachineUtilizationDto
                        {
                            MachineId = machineGroup.Key.MachineId,
                            MachineName = machineGroup.Key.MachineName,
                            MachineType = machineGroup.Key.Type,
                            SessionCount = machineGroup.Count(),
                            TotalMinutesRunning = machineGroup.Sum(x => x.TotalMinutes),
                            Revenue = machineGroup.Sum(x => x.PricePaid)
                        })
                        .OrderByDescending(m => m.SessionCount)
                        .ToList()
                })
                .OrderByDescending(s => s.TotalSessions)
                .ToList();

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 6. PEAK HOURS - Khung giờ cao điểm trong ngày
        //    Dùng để tối ưu nhân sự, giá giờ vàng, hoặc bảo trì ngoài giờ cao điểm
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<PeakHourDto>> GetPeakHoursAsync(Guid? brandId, Guid? storeId, int days)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            var query = _context.MachineSessions
                .Where(s => s.StartTime >= startDate &&
                            (s.Status == MachineSessionStatus.Completed || s.Status == MachineSessionStatus.Running));

            query = ApplyBrandStoreFilter(query, brandId, storeId);

            var raw = await query
                .Select(s => new { s.StartTime, s.PricePaid })
                .ToListAsync();

            var result = raw
                .GroupBy(x => x.StartTime.Hour)
                .Select(g => new PeakHourDto
                {
                    Hour = g.Key,
                    SessionCount = g.Count(),
                    AvgRevenue = g.Count() > 0 ? g.Sum(x => x.PricePaid) / g.Count() : 0
                })
                .OrderBy(h => h.Hour)
                .ToList();

            // Điền các giờ không có data (trả về 0) để biểu đồ đầy đủ 24 giờ
            var allHours = Enumerable.Range(0, 24)
                .Select(h => result.FirstOrDefault(r => r.Hour == h) ?? new PeakHourDto { Hour = h, SessionCount = 0, AvgRevenue = 0 })
                .ToList();

            return allHours;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 7. REVENUE TREND - Phân tích doanh thu theo thời gian
        // ─────────────────────────────────────────────────────────────────────

        public async Task<RevenueTrendResponseDto> GetRevenueTrendAsync(Guid? brandId, Guid? storeId, int days)
        {
            DateTime currentStart, currentEnd, prevStart, prevEnd;
            bool isHourly = days <= 1;

            if (days == 0) // Today
            {
                currentStart = DateTime.UtcNow.Date;
                currentEnd = DateTime.UtcNow;
                prevStart = currentStart.AddDays(-1);
                prevEnd = currentStart;
            }
            else if (days == 1) // Yesterday
            {
                currentStart = DateTime.UtcNow.Date.AddDays(-1);
                currentEnd = DateTime.UtcNow.Date;
                prevStart = currentStart.AddDays(-1);
                prevEnd = currentStart;
            }
            else // Week, Month, or custom
            {
                currentStart = DateTime.UtcNow.Date.AddDays(-days);
                currentEnd = DateTime.UtcNow;
                prevStart = currentStart.AddDays(-days);
                prevEnd = currentStart;
            }

            var query = _context.MachineSessions
                .Where(s => s.StartTime >= prevStart && s.StartTime < currentEnd && 
                            (s.Status == MachineSessionStatus.Completed || s.Status == MachineSessionStatus.Running));

            query = ApplyBrandStoreFilter(query, brandId, storeId);

            var rawData = await query
                .Select(s => new { s.StartTime, s.PricePaid })
                .ToListAsync();

            var currentPeriodRecords = rawData.Where(d => d.StartTime >= currentStart && d.StartTime < currentEnd).ToList();
            var prevPeriodRecords = rawData.Where(d => d.StartTime >= prevStart && d.StartTime < prevEnd).ToList();

            List<RevenueTrendPointDto> points;

            if (isHourly)
            {
                points = currentPeriodRecords
                    .GroupBy(x => new DateTime(x.StartTime.Year, x.StartTime.Month, x.StartTime.Day, x.StartTime.Hour, 0, 0))
                    .Select(g => new RevenueTrendPointDto
                    {
                        Date = g.Key,
                        Revenue = g.Sum(x => x.PricePaid),
                        SessionCount = g.Count()
                    })
                    .OrderBy(p => p.Date)
                    .ToList();
            }
            else
            {
                points = currentPeriodRecords
                    .GroupBy(x => x.StartTime.Date)
                    .Select(g => new RevenueTrendPointDto
                    {
                        Date = g.Key,
                        Revenue = g.Sum(x => x.PricePaid),
                        SessionCount = g.Count()
                    })
                    .OrderBy(p => p.Date)
                    .ToList();
            }

            decimal currentTotal = currentPeriodRecords.Sum(x => x.PricePaid);
            decimal prevTotal = prevPeriodRecords.Sum(x => x.PricePaid);

            double growthRate = prevTotal > 0 
                ? (double)((currentTotal - prevTotal) / prevTotal * 100) 
                : 0;

            return new RevenueTrendResponseDto
            {
                DataPoints = points,
                TotalRevenue = currentTotal,
                GrowthRate = Math.Round(growthRate, 1)
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // 8. MACHINE STATUS SUMMARY - Live Status Overview
        // ─────────────────────────────────────────────────────────────────────

        public async Task<MachineStatusSummaryDto> GetMachineStatusSummaryAsync(Guid? brandId, Guid? storeId)
        {
            var machinesQuery = _context.Machines.AsQueryable();

            if (storeId.HasValue)
                machinesQuery = machinesQuery.Where(m => m.StoreId == storeId.Value);
            else if (brandId.HasValue)
            {
                machinesQuery = machinesQuery
                    .Join(_context.Stores, m => m.StoreId, st => st.Id, (m, st) => new { m, st })
                    .Where(x => x.st.BrandId == brandId.Value)
                    .Select(x => x.m);
            }

            var totalMachines = await machinesQuery.CountAsync();

            // Lấy danh sách ID máy thuộc phạm vi lọc
            var machineIds = await machinesQuery.Select(m => m.Id).ToListAsync();

            // Tìm các máy đang có session "Running"
            var runningMachineIds = await _context.MachineSessions
                .Where(s => s.Status == MachineSessionStatus.Running && machineIds.Contains(s.MachineId))
                .Select(s => s.MachineId)
                .Distinct()
                .ToListAsync();

            // Tìm các máy có session cuối cùng bị "Error" (trong vòng 24h qua)
            var errorMachineIds = await _context.MachineSessions
                .Where(s => s.Status == MachineSessionStatus.Error && 
                            s.CreatedAt >= DateTime.UtcNow.AddHours(-24) &&
                            machineIds.Contains(s.MachineId))
                .Select(s => s.MachineId)
                .Distinct()
                .ToListAsync();

            // Loại trừ máy đang chạy khỏi danh sách lỗi
            var finalErrorCount = errorMachineIds.Except(runningMachineIds).Count();
            var runningCount = runningMachineIds.Count;
            
            // Tạm thời giả định Offline là 0 nếu không có heartbeat, 
            // Ready = Total - Running - Error
            var readyCount = Math.Max(0, totalMachines - runningCount - finalErrorCount);

            return new MachineStatusSummaryDto
            {
                Total = totalMachines,
                Running = runningCount,
                Error = finalErrorCount,
                Ready = readyCount,
                Offline = 0 // Cần tích hợp Heartbeat/IoT Cloud để lấy số này chính xác
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // 9. LEADERBOARD - Bảng xếp hạng doanh thu
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(string type, int top = 5)
        {
            var startDate = DateTime.UtcNow.AddDays(-30);

            if (type.ToLower() == "store")
            {
                return await _context.MachineSessions
                    .Where(s => s.StartTime >= startDate && s.Status == MachineSessionStatus.Completed)
                    .GroupBy(s => new { s.StoreId, Name = s.Store!.Name })
                    .Select(g => new LeaderboardEntryDto
                    {
                        Id = g.Key.StoreId,
                        Name = g.Key.Name,
                        Value = g.Sum(s => s.PricePaid)
                    })
                    .OrderByDescending(x => x.Value)
                    .Take(top)
                    .Select((x, index) => new LeaderboardEntryDto 
                    { 
                        Id = x.Id, Name = x.Name, Value = x.Value, Rank = index + 1 
                    })
                    .ToListAsync();
            }
            else // Default: Brand leaderboard
            {
                return await _context.MachineSessions
                    .Where(s => s.StartTime >= startDate && s.Status == MachineSessionStatus.Completed)
                    .GroupBy(s => new { s.Store!.BrandId, BrandName = s.Store.Brand!.Name })
                    .Select(g => new LeaderboardEntryDto
                    {
                        Id = g.Key.BrandId,
                        Name = g.Key.BrandName,
                        Value = g.Sum(s => s.PricePaid)
                    })
                    .OrderByDescending(x => x.Value)
                    .Take(top)
                    .Select((x, index) => new LeaderboardEntryDto 
                    { 
                        Id = x.Id, Name = x.Name, Value = x.Value, Rank = index + 1 
                    })
                    .ToListAsync();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 10. USER GROWTH - Tăng trưởng thành viên mới
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<UserGrowthPointDto>> GetUserGrowthAsync(int months)
        {
            var startMonth = DateTime.UtcNow.AddMonths(-months);
            
            var users = await _context.Users
                .Where(u => u.CreatedAt >= startMonth)
                .Select(u => new { u.CreatedAt })
                .ToListAsync();

            var totalBefore = await _context.Users.CountAsync(u => u.CreatedAt < startMonth);

            var monthlyData = users
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .ToList();

            var result = new List<UserGrowthPointDto>();
            int cumulative = totalBefore;

            foreach (var group in monthlyData)
            {
                int newUsers = group.Count();
                cumulative += newUsers;
                result.Add(new UserGrowthPointDto
                {
                    Month = $"{group.Key.Year}-{group.Key.Month:D2}",
                    NewUsers = newUsers,
                    TotalUsers = cumulative
                });
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPER
        // ─────────────────────────────────────────────────────────────────────

        private IQueryable<Models.MachineSession> ApplyBrandStoreFilter(
            IQueryable<Models.MachineSession> query, Guid? brandId, Guid? storeId)
        {
            if (storeId.HasValue)
                return query.Where(s => s.StoreId == storeId.Value);

            if (brandId.HasValue)
                return query.Where(s => s.Store!.BrandId == brandId.Value);

            return query;
        }
    }
}
