using QLS.Backend.DTOs.Loyalty;
using QLS.Backend.Models;

namespace QLS.Backend.Interfaces.Loyalty
{
    public interface ILoyaltyService
    {
        Task<PointClaimToken?> EnsureClaimTokenForPaymentAsync(MachineSession session, PaymentTransaction paymentTransaction);
        Task<LoyaltyClaimResponseDto> ClaimPointsAsync(Guid customerId, LoyaltyClaimRequestDto request);
        Task<LoyaltyMeResponseDto> GetMeAsync(Guid customerId);
        Task<IReadOnlyList<LoyaltyPointHistoryItemDto>> GetPointHistoryAsync(Guid customerId, int limit = 50);
        Task<LoyaltySessionInfoDto?> GetSessionLoyaltyInfoAsync(Guid sessionId, string baseClaimLinkUrl);
        Task<bool> RollbackEarnedPointsForSessionAsync(Guid machineSessionId, string reason);
        Task ProcessExpiredPointsAsync(CancellationToken cancellationToken = default);
    }
}
