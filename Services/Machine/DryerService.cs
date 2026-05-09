using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.Models;
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

        public async Task<Guid> SaveSessionAsync(CreateMachineSessionDto dto)
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
                Status       = MachineSessionStatus.PendingPayment, // 䌜ờ thanh toán - chưa chạy
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
            return session.Id;
        }

        public async Task<bool> UpdateSessionStatusAsync(Guid sessionId, MachineSessionStatus status, string? refundNote = null)
        {
            var session = await _context.MachineSessions.FindAsync(sessionId);
            if (session == null) return false;

            session.Status    = status;
            session.UpdatedAt = DateTime.UtcNow;

            switch (status)
            {
                case MachineSessionStatus.Completed:
                    // Máy hoàn thành — ghi nhận thời gian kết thúc thực tế
                    session.ActualEndTime = DateTime.UtcNow;
                    break;

                case MachineSessionStatus.Error:
                    // Máy lỗi — đánh dấu cần xử lý hoàn tiền
                    session.ActualEndTime = DateTime.UtcNow;
                    session.RefundStatus  = "Pending";
                    session.RefundNote    = refundNote ?? "Máy gặp sự cố trong lúc vận hành.";
                    break;

                case MachineSessionStatus.Cancelled:
                    // Hủy trước khi chạy — session đã ở PendingPayment nên không cần hoàn tiền
                    session.ActualEndTime = DateTime.UtcNow;
                    break;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Xác nhận thanh toán thành công: chuyển session từ PendingPayment → Running.
        /// Gọi sau khi payment gateway trả về thành công và máy bắt đầu chạy.
        /// </summary>
        public async Task<bool> ConfirmPaymentAsync(Guid sessionId, string? transactionId = null)
        {
            var session = await _context.MachineSessions.FindAsync(sessionId);
            if (session == null) return false;

            if (session.Status != MachineSessionStatus.PendingPayment)
                throw new InvalidOperationException(
                    $"Session không thể xác nhận: trạng thái hiận tại là '{session.Status}', chỉ có thể confirm khi PendingPayment.");

            var now = DateTime.UtcNow;
            session.Status             = MachineSessionStatus.Running;
            session.PaymentConfirmedAt = now;
            session.StartTime          = now;                          // Máy bắt đầu chạy từ lúc này
            session.EndTime            = now.AddMinutes(session.TotalMinutes);
            session.TransactionId      = transactionId;
            session.UpdatedAt          = now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<InitPaymentResponseDto> InitSessionAsync(InitPaymentRequestDto dto)
        {
            // 1. Lấy thông tin máy từ DB
            var machine = await _context.Machines.FindAsync(dto.MachineId);
            if (machine == null) throw new Exception("Không tìm thấy máy");

            // 2. Lấy giá chuẩn từ PricingService
            var capacityRaw = machine.Capacity ?? "0";
            var capacityClean = new string(capacityRaw.Where(c => char.IsDigit(c) || c == '.').ToArray());
            
            var priceResponse = await _pricingService.CalculatePriceAsync(new CalculatePriceRequestDto
            {
                StoreId = dto.StoreId,
                MachineType = machine.Type,
                MachineCapacityKg = decimal.TryParse(capacityClean, out var cap) ? cap : 0,
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

            // 4. LƯU DATABASE với trạng thái PendingPayment (chưa thu tiền, chưa chạy máy)
            var sessionDto = new CreateMachineSessionDto
            {
                BranchId    = dto.StoreId,
                MachineId   = dto.MachineId,
                UserId      = dto.UserId,
                TotalMinutes = totalMinutes,
                PricePaid   = totalAmount,
                PriceListId = priceResponse.PriceListId,
                PricingMode = priceResponse.Mode == "PerKg" ? PricePerType.PerKg : PricePerType.Flat,
                WeightKg    = dto.WeightKg,
                CycleName   = priceResponse.CalculationDetail, 
                IsExtension = false
            };

            var sessionId = await SaveSessionAsync(sessionDto);

            // 5. Trả về thông tin — client sẽ dùng SessionId này để confirm sau khi thanh toán
            return new InitPaymentResponseDto
            {
                SessionId              = sessionId,
                ServerCalculatedAmount = totalAmount, 
                TotalMinutes           = totalMinutes,
                PaymentMethod          = dto.PaymentMethod,
                Message                = "Session đã tạo. Vui lòng hoàn tất thanh toán để khởi động máy."
            };
        }
    }
}
