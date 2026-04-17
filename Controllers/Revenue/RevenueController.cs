using Microsoft.AspNetCore.Mvc;
using QLS.Backend.DTOs.Revenue;
using QLS.Backend.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QLS.Backend.Controllers.Revenue
{
    [ApiController]
    [Route("api/[controller]")]
    public class RevenueController : ControllerBase
    {
        private readonly IRevenueService _revenueService;

        public RevenueController(IRevenueService revenueService)
        {
            _revenueService = revenueService;
        }

        /// <summary>
        /// Lấy tổng quan doanh thu (Tổng tiền, Số phiên)
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<RevenueSummaryDto>> GetSummary(
            [FromQuery] Guid? brandId, 
            [FromQuery] Guid? storeId, 
            [FromQuery] DateTime? startTime, 
            [FromQuery] DateTime? endTime)
        {
            var summary = await _revenueService.GetRevenueSummaryAsync(brandId, storeId, startTime, endTime);
            return Ok(summary);
        }

        /// <summary>
        /// Báo cáo doanh thu theo từng ngày
        /// </summary>
        [HttpGet("daily")]
        public async Task<ActionResult<List<DailyRevenueDto>>> GetDailyReport(
            [FromQuery] Guid? brandId, 
            [FromQuery] Guid? storeId, 
            [FromQuery] int days = 7)
        {
            var report = await _revenueService.GetDailyRevenueReportAsync(brandId, storeId, days);
            return Ok(report);
        }

        /// <summary>
        /// So sánh doanh thu giữa các cửa hàng thuộc một Brand
        /// </summary>
        [HttpGet("brand/{brandId}/stores")]
        public async Task<ActionResult<List<StoreRevenueDto>>> GetBrandStoresRevenue(
            Guid brandId, 
            [FromQuery] DateTime? startTime, 
            [FromQuery] DateTime? endTime)
        {
            var report = await _revenueService.GetStoresRevenueAsync(brandId, startTime, endTime);
            return Ok(report);
        }

        /// <summary>
        /// Xếp hạng doanh thu các máy trong một cửa hàng
        /// </summary>
        [HttpGet("store/{storeId}/machines")]
        public async Task<ActionResult<List<MachineRevenueRankingDto>>> GetMachineRanking(
            Guid storeId, 
            [FromQuery] DateTime? startTime, 
            [FromQuery] DateTime? endTime)
        {
            var report = await _revenueService.GetMachineRankingAsync(storeId, startTime, endTime);
            return Ok(report);
        }
    }
}
