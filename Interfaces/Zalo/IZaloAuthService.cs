using QLS.Backend.DTOs.Zalo;

namespace QLS.Backend.Interfaces.Zalo
{
    public interface IZaloAuthService
    {
        Task<ZaloLoginResponseDto> LoginAsync(ZaloLoginRequestDto request);
    }
}
