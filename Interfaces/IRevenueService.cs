using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QLS.Backend.DTOs.Revenue;

namespace QLS.Backend.Interfaces
{
    public interface IRevenueService
    {
        Task<RevenueSummaryDto> GetRevenueSummaryAsync(Guid? brandId, Guid? storeId, DateTime? startTime, DateTime? endTime);
        Task<List<DailyRevenueDto>> GetDailyRevenueReportAsync(Guid? brandId, Guid? storeId, int days);
        Task<List<StoreRevenueDto>> GetStoresRevenueAsync(Guid brandId, DateTime? startTime, DateTime? endTime);
        Task<List<MachineRevenueRankingDto>> GetMachineRankingAsync(Guid storeId, DateTime? startTime, DateTime? endTime);
    }
}
