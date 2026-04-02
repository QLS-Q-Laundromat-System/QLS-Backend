using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.Models;
using QLS.Backend.DTOs.Store;
using System.Threading.Tasks;
using System;

namespace QLS.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BranchSettingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BranchSettingsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// [POST] api/branchsettings
        /// Thêm mới hoặc Cập nhật cài đặt của một chi nhánh (Upsert)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveSettings([FromBody] BranchSettingDto request)
        {
            if (request.BranchId == Guid.Empty)
            {
                return BadRequest(new { message = "BranchId không hợp lệ." });
            }

            try
            {
                // 1. Tìm xem chi nhánh này đã có dòng cài đặt nào trong DB chưa
                var existingSetting = await _context.BranchSettings
                    .FirstOrDefaultAsync(s => s.BranchId == request.BranchId);

                if (existingSetting == null)
                {
                    // 2A. Nếu CHƯA CÓ -> Tạo mới (Insert)
                    var newSetting = new BranchSetting
                    {
                        BranchId                = request.BranchId,
                        DryerStepMinutes        = request.DryerStepMinutes,
                        DryerStepPrice          = request.DryerStepPrice,
                        DryerMinInitialMinutes  = request.DryerMinInitialMinutes,
                        DryerGracePeriodMinutes = request.DryerGracePeriodMinutes
                    };

                    _context.BranchSettings.Add(newSetting);
                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Đã tạo mới cài đặt thành công!", data = newSetting });
                }
                else
                {
                    // 2B. Nếu ĐÃ CÓ -> Cập nhật (Update)
                    existingSetting.DryerStepMinutes        = request.DryerStepMinutes;
                    existingSetting.DryerStepPrice          = request.DryerStepPrice;
                    existingSetting.DryerMinInitialMinutes  = request.DryerMinInitialMinutes;
                    existingSetting.DryerGracePeriodMinutes = request.DryerGracePeriodMinutes;

                    _context.BranchSettings.Update(existingSetting);
                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Đã cập nhật cài đặt thành công!", data = existingSetting });
                }
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lưu cài đặt: " + ex.Message });
            }
        }

        /// <summary>
        /// [GET] api/branchsettings/{branchId}
        /// Lấy cài đặt của một chi nhánh để React FE đổ vào Form chỉnh sửa
        /// </summary>
        [HttpGet("{branchId}")]
        public async Task<IActionResult> GetSettings(Guid branchId)
        {
            var setting = await _context.BranchSettings
                .FirstOrDefaultAsync(s => s.BranchId == branchId);

            if (setting == null)
            {
                return NotFound(new { message = "Chưa có cài đặt nào cho chi nhánh này." });
            }

            return Ok(setting);
        }
    }
}
