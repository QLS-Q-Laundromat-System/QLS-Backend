using QLS.Backend.DTOs.Loyalty.Auth;

namespace QLS.Backend.Interfaces.Loyalty
{
    public interface ILoyaltyAuthService
    {
        Task<LoyaltyOtpRequestResponseDto> RequestOtpAsync(LoyaltyOtpRequestDto request);
        Task<LoyaltyAuthResponseDto> RegisterAsync(LoyaltyRegisterRequestDto request);
        Task<LoyaltyAuthResponseDto> LoginWithPasswordAsync(LoyaltyPasswordLoginRequestDto request);
        Task<LoyaltyAuthResponseDto> LoginWithOtpAsync(LoyaltyOtpLoginRequestDto request);
    }
}
