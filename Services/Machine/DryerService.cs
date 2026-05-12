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
using Microsoft.Extensions.Configuration;

namespace QLS.Backend.Services
{
    public class DryerService : IDryerService
    {
        private readonly AppDbContext _context;
        private readonly IPricingCalculatorService _pricingService;
        private readonly IConfiguration _configuration;

        public DryerService(AppDbContext context, IPricingCalculatorService pricingService, IConfiguration configuration)
        {
            _context = context;
            _pricingService = pricingService;
            _configuration = configuration;
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
                Status       = MachineSessionStatus.PendingPayment,
                CreatedAt    = now,
                UpdatedAt    = now,
                PriceListId  = dto.PriceListId,
                PricingMode  = dto.PricingMode,
                WeightKg     = dto.WeightKg,
                CycleName    = dto.CycleName,
                IsExtension  = dto.IsExtension,
                PaymentCode  = dto.PaymentCode
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
                    session.ActualEndTime = DateTime.UtcNow;
                    break;
                case MachineSessionStatus.Error:
                    session.ActualEndTime = DateTime.UtcNow;
                    session.RefundStatus  = "Pending";
                    session.RefundNote    = refundNote ?? "Máy gặp sự cố trong lúc vận hành.";
                    break;
                case MachineSessionStatus.Cancelled:
                    session.ActualEndTime = DateTime.UtcNow;
                    break;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ConfirmPaymentAsync(Guid sessionId, string? transactionId = null)
        {
            var session = await _context.MachineSessions.FindAsync(sessionId);
            if (session == null) return false;

            if (session.Status != MachineSessionStatus.PendingPayment)
                throw new InvalidOperationException($"Session không thể xác nhận: trạng thái hiện tại là '{session.Status}'");

            var now = DateTime.UtcNow;
            session.Status             = MachineSessionStatus.Running;
            session.PaymentConfirmedAt = now;
            session.StartTime          = now;
            session.EndTime            = now.AddMinutes(session.TotalMinutes);
            session.TransactionId      = transactionId;
            session.UpdatedAt          = now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<InitPaymentResponseDto> InitSessionAsync(InitPaymentRequestDto dto)
        {
            var machine = await _context.Machines.FindAsync(dto.MachineId);
            if (machine == null) throw new Exception("Không tìm thấy máy");

            var capacityRaw = machine.Capacity ?? "0";
            var capacityClean = new string(capacityRaw.Where(c => char.IsDigit(c) || c == '.').ToArray());
            
            var priceResponse = await _pricingService.CalculatePriceAsync(new CalculatePriceRequestDto
            {
                StoreId = dto.StoreId,
                MachineType = machine.Type,
                MachineCapacityKg = decimal.TryParse(capacityClean, out var cap) ? cap : 0,
                ClothingWeightKg = dto.WeightKg
            });

            if (priceResponse == null) throw new Exception("Không tìm thấy bảng giá.");

            decimal totalAmount = 0;
            int totalMinutes = 0;

            if (machine.Type == MachineType.Dryer)
            {
                if (!dto.RequestedSteps.HasValue || !priceResponse.MinInitialSteps.HasValue)
                    throw new Exception("Thiếu thông tin số bước sấy.");
                totalAmount = dto.RequestedSteps.Value * priceResponse.FinalPrice;
                totalMinutes = dto.RequestedSteps.Value * (priceResponse.DurationMinutes ?? 0);
            }
            else
            {
                totalAmount = priceResponse.FinalPrice;
                totalMinutes = priceResponse.DurationMinutes ?? 0;
            }

            var paymentCode = $"QLS{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}";

            var sessionDto = new CreateMachineSessionDto
            {
                BranchId     = dto.StoreId,
                MachineId    = dto.MachineId,
                UserId       = dto.UserId,
                TotalMinutes = totalMinutes,
                PricePaid    = totalAmount,
                PriceListId  = priceResponse.PriceListId,
                PricingMode  = priceResponse.Mode == "PerKg" ? PricePerType.PerKg : PricePerType.Flat,
                WeightKg     = dto.WeightKg,
                CycleName    = priceResponse.CalculationDetail, 
                IsExtension  = false,
                PaymentCode  = paymentCode
            };

            var sessionId = await SaveSessionAsync(sessionDto);

            var acc = _configuration["SePay:AccountNumber"];
            var bank = _configuration["SePay:Bank"] ?? "MBBank";
            var qrUrl = $"https://qr.sepay.vn/img?acc={acc}&bank={bank}&amount={(int)totalAmount}&des={paymentCode}";

            return new InitPaymentResponseDto
            {
                SessionId              = sessionId,
                ServerCalculatedAmount = totalAmount, 
                TotalMinutes           = totalMinutes,
                PaymentMethod          = dto.PaymentMethod,
                PaymentCode            = paymentCode,
                QrUrl                  = qrUrl,
                Message                = "Session đã tạo. Vui lòng hoàn tất thanh toán để khởi động máy."
            };
        }
    }
}
