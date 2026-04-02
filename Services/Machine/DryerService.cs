using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.Models;
using QLS.Backend.DTOs.Dryer;

using QLS.Backend.Interfaces;

namespace QLS.Backend.Services
{
    public class DryerService : IDryerService
    {
        private readonly AppDbContext _context;

        public DryerService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DryerOptionResponseDto> GetDryerOptionsAsync(Guid branchId, string machineId, Guid userId)
        {
            // 1. Lấy bộ Cài đặt của Cửa hàng (Store)
            var setting = await _context.StoreSettings
                .FirstOrDefaultAsync(s => s.StoreId == branchId);
                
            if (setting == null) throw new Exception("Không tìm thấy cài đặt cho cửa hàng này!");

            // 2. Tìm lịch sử sấy GẦN NHẤT của cái máy sấy này
            var latestSession = await _context.MachineSessions
                .Where(m => m.MachineId == machineId)
                .OrderByDescending(m => m.EndTime)
                .FirstOrDefaultAsync();

            var now = DateTime.UtcNow;
            bool isExtendable = false;

            // 3. Logic xét duyệt 10 phút "vàng"
            if (latestSession != null)
            {
                // Kiểm tra xem có đúng là người dùng đó không?
                bool isSameUser = latestSession.UserId == userId;
                
                // Tính thời hạn chót để được sấy tiếp (EndTime + 10 phút)
                var gracePeriodEnd = latestSession.EndTime.AddMinutes(setting.DryerGracePeriodMinutes);

                // Nếu máy ĐÃ XONG và BÂY GIỜ vẫn nằm trong khoảng 10 phút sau khi xong
                if (isSameUser && now >= latestSession.EndTime && now <= gracePeriodEnd)
                {
                    isExtendable = true; // Bật cờ cho phép sấy tiếp!
                }
            }

            // 4. Đóng gói kết quả trả về
            if (isExtendable)
            {
                return new DryerOptionResponseDto 
                {
                    IsExtendSession = true,
                    MinMinutesAllowed = setting.DryerStepMinutes,
                    StepMinutes = setting.DryerStepMinutes,
                    PricePerStep = setting.DryerStepPrice
                };
            }
            else
            {
                return new DryerOptionResponseDto
                {
                    IsExtendSession = false,
                    MinMinutesAllowed = setting.DryerMinInitialMinutes,
                    StepMinutes = setting.DryerStepMinutes,
                    PricePerStep = setting.DryerStepPrice
                };
            }
        }
        public async Task SaveSessionAsync(Guid branchId, string machineId, Guid userId, int minutes)
        {
            var now = DateTime.UtcNow;
            var session = new MachineSession
            {
                Id        = Guid.NewGuid(),
                MachineId = machineId,
                UserId    = userId,
                StartTime = now,
                EndTime   = now.AddSeconds(minutes * 60),
                Status    = 0 // 0 = Đang chạy (Running)
            };

            _context.MachineSessions.Add(session);
            await _context.SaveChangesAsync();
        }
    }
}
