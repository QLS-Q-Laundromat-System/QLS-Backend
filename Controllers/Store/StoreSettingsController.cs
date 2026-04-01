using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.Models;
using QLS.Backend.DTOs.Store;
using System.Threading.Tasks;

namespace QLS.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoreSettingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StoreSettingsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// [POST] api/storesettings
        /// Thêm mới hoặc Cập nhật cài đặt của một cửa hàng (Upsert)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveSettings([FromBody] StoreSettingDto request)
        {
            if (string.IsNullOrEmpty(request.StoreId))
            {
                return BadRequest(new { message = "StoreId không hợp lệ." });
            }

            try
            {
                // 1. Tìm xem cửa hàng này đã có dòng cài đặt nào trong DB chưa
                var existingSetting = await _context.StoreSettings
                    .FirstOrDefaultAsync(s => s.StoreId == request.StoreId);

                if (existingSetting == null)
                {
                    // 2A. Nếu CHƯA CÓ -> Tạo mới (Insert)
                    var newSetting = new StoreSetting
                    {
                        StoreId                 = request.StoreId,
                        DryerStepMinutes        = request.DryerStepMinutes,
                        DryerStepPrice          = request.DryerStepPrice,
                        DryerMinInitialMinutes  = request.DryerMinInitialMinutes,
                        DryerGracePeriodMinutes = request.DryerGracePeriodMinutes
                    };

                    _context.StoreSettings.Add(newSetting);
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

                    _context.StoreSettings.Update(existingSetting);
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
        /// [GET] api/storesettings/{storeId}
        /// Lấy cài đặt của một cửa hàng để React FE đổ vào Form chỉnh sửa
        /// </summary>
        [HttpGet("{storeId}")]
        public async Task<IActionResult> GetSettings(string storeId)
        {
            var setting = await _context.StoreSettings
                .FirstOrDefaultAsync(s => s.StoreId == storeId);

            if (setting == null)
            {
                return NotFound(new { message = "Chưa có cài đặt nào cho cửa hàng này." });
            }

            return Ok(setting);
        }
    }
}
