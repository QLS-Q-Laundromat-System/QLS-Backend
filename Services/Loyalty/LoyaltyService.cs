using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.DTOs.Loyalty;
using QLS.Backend.Exceptions;
using QLS.Backend.Interfaces.Loyalty;
using QLS.Backend.Models;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Services.Loyalty
{
    public class LoyaltyService : ILoyaltyService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public LoyaltyService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<PointClaimToken?> EnsureClaimTokenForPaymentAsync(MachineSession session, PaymentTransaction paymentTransaction)
        {
            var existingToken = await _context.PointClaimTokens
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(t => t.MachineSessionId == session.Id);

            if (existingToken != null)
            {
                return existingToken;
            }

            var brandId = await _context.Stores
                .Where(s => s.Id == session.StoreId)
                .Select(s => s.BrandId)
                .FirstOrDefaultAsync();

            if (brandId == Guid.Empty)
            {
                throw new ApiException("Không tìm thấy Brand theo session để tạo token nhận điểm.", 400);
            }

            var points = CalculatePoints(session.PricePaid);
            if (points <= 0)
            {
                return null;
            }

            var ttlMinutes = GetClaimTokenTtlMinutes();
            var token = new PointClaimToken
            {
                Token = GenerateToken(),
                BrandId = brandId,
                MachineSessionId = session.Id,
                PaymentTransactionId = paymentTransaction.Id,
                PaidAmount = session.PricePaid,
                PointsToEarn = points,
                IsClaimed = false,
                ExpiredAt = DateTime.UtcNow.AddMinutes(ttlMinutes),
                CreatedAt = DateTime.UtcNow
            };

            _context.PointClaimTokens.Add(token);
            await _context.SaveChangesAsync();

            return token;
        }

        public async Task<LoyaltyClaimResponseDto> ClaimPointsAsync(Guid customerId, LoyaltyClaimRequestDto request)
        {
            var customer = await _context.LoyaltyCustomers
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer == null)
            {
                throw new ApiException("Không tìm thấy khách hàng loyalty.", 404);
            }

            var claimToken = request.ClaimToken.Trim();
            var token = await _context.PointClaimTokens
                .Include(t => t.MachineSession)
                .FirstOrDefaultAsync(t => t.Token == claimToken);

            if (token == null)
            {
                throw new ApiException("Token nhận điểm không tồn tại.", 404);
            }

            if (token.BrandId != customer.BrandId)
            {
                throw new ApiException("Token không thuộc Brand của khách hàng hiện tại.", 403);
            }

            if (token.IsClaimed)
            {
                throw new ApiException("Token này đã được sử dụng.", 409);
            }

            if (token.ExpiredAt <= DateTime.UtcNow)
            {
                throw new ApiException("Token đã hết hạn.", 400);
            }

            if (token.PointsToEarn <= 0)
            {
                throw new ApiException("Token không hợp lệ vì số điểm nhận bằng 0.", 400);
            }

            var session = token.MachineSession;
            if (session == null)
            {
                throw new ApiException("Không tìm thấy session tương ứng với token.", 400);
            }

            if (session.PaymentConfirmedAt == null && session.Status == MachineSessionStatus.PendingPayment)
            {
                throw new ApiException("Session chưa thanh toán thành công.", 400);
            }

            if (session.Status == MachineSessionStatus.Cancelled)
            {
                throw new ApiException("Session đã bị hủy, không thể cộng điểm.", 400);
            }

            if (session.Status == MachineSessionStatus.Error)
            {
                throw new ApiException("Session bị lỗi, không thể cộng điểm.", 400);
            }

            var alreadyEarned = await _context.LoyaltyPointTransactions
                .AnyAsync(t => t.MachineSessionId == session.Id && t.Type == PointTransactionType.Earn);

            if (alreadyEarned)
            {
                throw new ApiException("Session này đã được cộng điểm trước đó.", 409);
            }

            var pointExpiryMonths = GetPointExpiryMonths();
            var now = DateTime.UtcNow;

            var earnTransaction = new LoyaltyPointTransaction
            {
                BrandId = customer.BrandId,
                CustomerId = customer.Id,
                MachineSessionId = session.Id,
                Type = PointTransactionType.Earn,
                Points = token.PointsToEarn,
                RemainingPoints = token.PointsToEarn,
                ExpiredAt = now.AddMonths(pointExpiryMonths),
                Note = $"Earn từ session {session.Id}",
                CreatedAt = now
            };

            customer.TotalPoints += token.PointsToEarn;
            customer.UpdatedAt = now;

            token.IsClaimed = true;
            token.ClaimedByCustomerId = customer.Id;
            token.ClaimedAt = now;

            _context.LoyaltyPointTransactions.Add(earnTransaction);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new ApiException("Session này đã được cộng điểm bởi yêu cầu khác.", 409);
            }

            return new LoyaltyClaimResponseDto
            {
                CustomerId = customer.Id,
                MachineSessionId = session.Id,
                ClaimedPoints = token.PointsToEarn,
                TotalPoints = customer.TotalPoints,
                ClaimedAt = now
            };
        }

        public async Task<LoyaltyMeResponseDto> GetMeAsync(Guid customerId)
        {
            var customer = await _context.LoyaltyCustomers
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer == null)
            {
                throw new ApiException("Không tìm thấy khách hàng loyalty.", 404);
            }

            return new LoyaltyMeResponseDto
            {
                CustomerId = customer.Id,
                BrandId = customer.BrandId,
                Email = customer.Email,
                PhoneNumber = customer.PhoneNumber,
                IsEmailVerified = customer.IsEmailVerified,
                IsPhoneNumberVerified = customer.IsPhoneNumberVerified,
                FullName = customer.FullName,
                AvatarUrl = customer.AvatarUrl,
                CustomerType = customer.CustomerType.ToString(),
                StudentVerificationStatus = customer.StudentVerificationStatus.ToString(),
                TotalPoints = customer.TotalPoints
            };
        }

        public async Task<IReadOnlyList<LoyaltyPointHistoryItemDto>> GetPointHistoryAsync(Guid customerId, int limit = 50)
        {
            var normalizedLimit = Math.Clamp(limit, 1, 200);

            var items = await _context.LoyaltyPointTransactions
                .Where(t => t.CustomerId == customerId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(normalizedLimit)
                .Select(t => new LoyaltyPointHistoryItemDto
                {
                    Id = t.Id,
                    Type = t.Type.ToString(),
                    Points = t.Points,
                    RemainingPoints = t.RemainingPoints,
                    MachineSessionId = t.MachineSessionId,
                    ExpiredAt = t.ExpiredAt,
                    Note = t.Note,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return items;
        }

        public async Task<LoyaltySessionInfoDto?> GetSessionLoyaltyInfoAsync(Guid sessionId, string baseClaimLinkUrl)
        {
            var token = await _context.PointClaimTokens
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(t => t.MachineSessionId == sessionId);

            if (token == null)
            {
                return null;
            }

            var baseUrl = baseClaimLinkUrl.TrimEnd('/');
            var claimQrUrl = $"{baseUrl}/{Uri.EscapeDataString(token.Token)}";

            return new LoyaltySessionInfoDto
            {
                ClaimQrUrl = claimQrUrl,
                ExpiredAt = token.ExpiredAt,
                PointsToEarn = token.PointsToEarn,
                IsClaimed = token.IsClaimed
            };
        }

        public async Task<bool> RollbackEarnedPointsForSessionAsync(Guid machineSessionId, string reason)
        {
            var earn = await _context.LoyaltyPointTransactions
                .Include(t => t.Customer)
                .FirstOrDefaultAsync(t => t.MachineSessionId == machineSessionId && t.Type == PointTransactionType.Earn);

            if (earn == null || earn.Customer == null)
            {
                return false;
            }

            if (earn.RemainingPoints <= 0)
            {
                return false;
            }

            var alreadyRolledBack = await _context.LoyaltyPointTransactions
                .AnyAsync(t =>
                    t.MachineSessionId == machineSessionId &&
                    t.Type == PointTransactionType.Adjust &&
                    t.Note != null &&
                    t.Note.StartsWith("Rollback điểm:"));

            if (alreadyRolledBack)
            {
                return false;
            }

            var pointsToRollback = Math.Min(earn.RemainingPoints, Math.Max(0, earn.Customer.TotalPoints));
            if (pointsToRollback <= 0)
            {
                earn.RemainingPoints = 0;
                await _context.SaveChangesAsync();
                return false;
            }

            earn.Customer.TotalPoints -= pointsToRollback;
            earn.Customer.UpdatedAt = DateTime.UtcNow;
            earn.RemainingPoints -= pointsToRollback;

            var rollbackTransaction = new LoyaltyPointTransaction
            {
                BrandId = earn.BrandId,
                CustomerId = earn.CustomerId,
                MachineSessionId = machineSessionId,
                Type = PointTransactionType.Adjust,
                Points = -pointsToRollback,
                RemainingPoints = 0,
                ExpiredAt = null,
                Note = $"Rollback điểm: {reason}",
                CreatedAt = DateTime.UtcNow
            };

            _context.LoyaltyPointTransactions.Add(rollbackTransaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ProcessExpiredPointsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var expiredEarnTransactions = await _context.LoyaltyPointTransactions
                .Include(t => t.Customer)
                .Where(t =>
                    t.Type == PointTransactionType.Earn &&
                    t.RemainingPoints > 0 &&
                    t.ExpiredAt != null &&
                    t.ExpiredAt <= now)
                .ToListAsync(cancellationToken);

            if (!expiredEarnTransactions.Any())
            {
                return;
            }

            foreach (var earn in expiredEarnTransactions)
            {
                if (earn.Customer == null)
                {
                    continue;
                }

                var expiredPoints = Math.Min(earn.RemainingPoints, Math.Max(0, earn.Customer.TotalPoints));
                earn.RemainingPoints = 0;

                if (expiredPoints <= 0)
                {
                    continue;
                }

                earn.Customer.TotalPoints -= expiredPoints;
                earn.Customer.UpdatedAt = now;

                _context.LoyaltyPointTransactions.Add(new LoyaltyPointTransaction
                {
                    BrandId = earn.BrandId,
                    CustomerId = earn.CustomerId,
                    MachineSessionId = earn.MachineSessionId,
                    Type = PointTransactionType.Expire,
                    Points = -expiredPoints,
                    RemainingPoints = 0,
                    ExpiredAt = null,
                    Note = $"Điểm hết hạn từ giao dịch {earn.Id}",
                    CreatedAt = now
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private int CalculatePoints(decimal amount)
        {
            var pointUnitVnd = GetPointUnitVnd();
            if (pointUnitVnd <= 0)
            {
                pointUnitVnd = 10000;
            }

            return (int)Math.Floor(amount / pointUnitVnd);
        }

        private int GetClaimTokenTtlMinutes()
        {
            return int.TryParse(_configuration["Loyalty:ClaimTokenTtlMinutes"], out var ttl) && ttl > 0 ? ttl : 10;
        }

        private int GetPointUnitVnd()
        {
            return int.TryParse(_configuration["Loyalty:PointUnitVnd"], out var pointUnit) && pointUnit > 0 ? pointUnit : 10000;
        }

        private int GetPointExpiryMonths()
        {
            return int.TryParse(_configuration["Loyalty:PointExpiryMonths"], out var months) && months > 0 ? months : 3;
        }

        private static string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(24);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
