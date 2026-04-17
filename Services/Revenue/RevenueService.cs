using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.DTOs.Revenue;
using QLS.Backend.Interfaces;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Services.Revenue
{
    public class RevenueService : IRevenueService
    {
        private readonly AppDbContext _context;

        public RevenueService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RevenueSummaryDto> GetRevenueSummaryAsync(Guid? brandId, Guid? storeId, DateTime? startTime, DateTime? endTime)
        {
            var query = _context.MachineSessions
                .Where(s => s.Status == MachineSessionStatus.Completed || s.Status == MachineSessionStatus.Running);

            if (storeId.HasValue)
            {
                query = query.Where(s => s.StoreId == storeId.Value);
            }
            else if (brandId.HasValue)
            {
                // Join with Store to filter by BrandId
                query = query.Where(s => s.Store!.BrandId == brandId.Value);
            }

            if (startTime.HasValue)
                query = query.Where(s => s.StartTime >= startTime.Value);

            if (endTime.HasValue)
                query = query.Where(s => s.StartTime <= endTime.Value);

            var result = await query.Select(s => s.PricePaid).ToListAsync();

            return new RevenueSummaryDto
            {
                TotalRevenue = result.Sum(),
                TotalSessions = result.Count
            };
        }

        public async Task<List<DailyRevenueDto>> GetDailyRevenueReportAsync(Guid? brandId, Guid? storeId, int days)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);

            var query = _context.MachineSessions
                .Where(s => (s.Status == MachineSessionStatus.Completed || s.Status == MachineSessionStatus.Running) 
                            && s.StartTime >= startDate);

            if (storeId.HasValue)
            {
                query = query.Where(s => s.StoreId == storeId.Value);
            }
            else if (brandId.HasValue)
            {
                query = query.Where(s => s.Store!.BrandId == brandId.Value);
            }

            var result = await query
                .GroupBy(s => s.StartTime.Date)
                .Select(g => new DailyRevenueDto
                {
                    Date = g.Key,
                    Revenue = g.Sum(s => s.PricePaid),
                    SessionCount = g.Count()
                })
                .OrderBy(r => r.Date)
                .ToListAsync();

            return result;
        }

        public async Task<List<StoreRevenueDto>> GetStoresRevenueAsync(Guid brandId, DateTime? startTime, DateTime? endTime)
        {
            var query = _context.MachineSessions
                .Where(s => s.Store!.BrandId == brandId && 
                            (s.Status == MachineSessionStatus.Completed || s.Status == MachineSessionStatus.Running));

            if (startTime.HasValue)
                query = query.Where(s => s.StartTime >= startTime.Value);

            if (endTime.HasValue)
                query = query.Where(s => s.StartTime <= endTime.Value);

            var result = await query
                .GroupBy(s => new { s.StoreId, s.Store!.Name })
                .Select(g => new StoreRevenueDto
                {
                    StoreId = g.Key.StoreId,
                    StoreName = g.Key.Name,
                    TotalRevenue = g.Sum(s => s.PricePaid),
                    SessionCount = g.Count()
                })
                .OrderByDescending(r => r.TotalRevenue)
                .ToListAsync();

            return result;
        }

        public async Task<List<MachineRevenueRankingDto>> GetMachineRankingAsync(Guid storeId, DateTime? startTime, DateTime? endTime)
        {
            var query = _context.MachineSessions
                .Where(s => s.StoreId == storeId && 
                            (s.Status == MachineSessionStatus.Completed || s.Status == MachineSessionStatus.Running));

            if (startTime.HasValue)
                query = query.Where(s => s.StartTime >= startTime.Value);

            if (endTime.HasValue)
                query = query.Where(s => s.StartTime <= endTime.Value);

            var result = await query
                .GroupBy(s => new { s.MachineId, s.Machine!.Name })
                .Select(g => new MachineRevenueRankingDto
                {
                    MachineId = g.Key.MachineId,
                    MachineName = g.Key.Name,
                    Revenue = g.Sum(s => s.PricePaid),
                    SessionCount = g.Count()
                })
                .OrderByDescending(r => r.Revenue)
                .ToListAsync();

            return result;
        }
    }
}
