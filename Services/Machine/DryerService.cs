using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.Models;
using QLS.Backend.DTOs.Dryer;
using QLS.Backend.DTOs.Machine;

using QLS.Backend.Interfaces;

using QLS.Backend.Interfaces.Pricing;
using QLS.Backend.DTOs.Pricing;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Services
{
    public class DryerService : IDryerService
    {
        private readonly AppDbContext _context;
        private readonly IPricingCalculatorService _pricingService;

        public DryerService(AppDbContext context, IPricingCalculatorService pricingService)
        {
            _context = context;
            _pricingService = pricingService;
        }

        public async Task<DryerOptionResponseDto> GetDryerOptionsAsync(Guid branchId, Guid machineId, Guid userId)
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
        public async Task SaveSessionAsync(CreateMachineSessionDto dto)
        {
            var now = DateTime.UtcNow;
            var session = new MachineSession
            {
                Id           = Guid.NewGuid(),
                MachineId    = dto.MachineId,
                StoreId      = dto.BranchId,
                UserId       = dto.UserId,
                PricePaid    = dto.PricePaid,
                TaxAmount    = 0, 
                TotalMinutes = dto.TotalMinutes,
                StartTime    = now,
                EndTime      = now.AddMinutes(dto.TotalMinutes),
                Status       = MachineSessionStatus.Running,
                CreatedAt    = now,
                UpdatedAt    = now,
                
                // Các trường mới bổ sung
                PriceListId  = dto.PriceListId,
                PricingMode  = dto.PricingMode,
                WeightKg     = dto.WeightKg,
                CycleName    = dto.CycleName,
                IsExtension  = dto.IsExtension
            };

            _context.MachineSessions.Add(session);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateSessionStatusAsync(Guid sessionId, MachineSessionStatus status)
        {
            var session = await _context.MachineSessions.FindAsync(sessionId);
            if (session == null) return false;

            session.Status = status;
            session.UpdatedAt = DateTime.UtcNow;

            if (status == MachineSessionStatus.Completed)
            {
                session.ActualEndTime = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<InitPaymentResponseDto> InitSessionAsync(InitPaymentRequestDto dto)
        {
            // 1. Lấy thông tin máy từ DB
            var machine = await _context.Machines.FindAsync(dto.MachineId);
            if (machine == null) throw new Exception("Không tìm thấy máy");

            // 2. Lấy giá chuẩn từ PricingService
            var priceResponse = await _pricingService.CalculatePriceAsync(new CalculatePriceRequestDto
            {
                StoreId = dto.StoreId,
                MachineType = machine.Type,
                MachineCapacityKg = decimal.TryParse(machine.Capacity, out var cap) ? cap : 0,
                ClothingWeightKg = dto.WeightKg
            });

            if (priceResponse == null) 
                throw new Exception("Không tìm thấy bảng giá hoặc máy không hỗ trợ.");

            decimal totalAmount = 0;
            int totalMinutes = 0;

            // 3. TÍNH TOÁN THEO LOẠI MÁY
            if (machine.Type == MachineType.Dryer)
            {
                if (!dto.RequestedSteps.HasValue || !priceResponse.MinInitialSteps.HasValue)
                    throw new Exception("Thiếu thông tin số bước sấy.");

                if (dto.RequestedSteps.Value < priceResponse.MinInitialSteps.Value)
                    throw new Exception($"Số bước tối thiểu là {priceResponse.MinInitialSteps.Value}");

                totalAmount = dto.RequestedSteps.Value * priceResponse.FinalPrice;
                totalMinutes = dto.RequestedSteps.Value * (priceResponse.DurationMinutes ?? 0);
            }
            else // Washer
            {
                totalAmount = priceResponse.FinalPrice;
                totalMinutes = priceResponse.DurationMinutes ?? 0;
            }

            // 4. LƯU DATABASE
            var sessionDto = new CreateMachineSessionDto
            {
                BranchId = dto.StoreId,
                MachineId = dto.MachineId,
                UserId = dto.UserId,
                TotalMinutes = totalMinutes,
                PricePaid = totalAmount,
                PriceListId = priceResponse.PriceListId,
                PricingMode = priceResponse.Mode == "PerKg" ? PricePerType.PerKg : PricePerType.Flat,
                WeightKg = dto.WeightKg,
                CycleName = priceResponse.CalculationDetail, 
                IsExtension = false
            };

            await SaveSessionAsync(sessionDto);

            // 5. Trả về thông tin
            return new InitPaymentResponseDto
            {
                ServerCalculatedAmount = totalAmount, 
                TotalMinutes = totalMinutes,
                PaymentMethod = dto.PaymentMethod,
                Message = "Session đã khởi tạo, chờ thanh toán."
            };
        }
    }
}

