using QLS.Backend.DTOs.Zalo;

namespace QLS.Backend.Interfaces.Zalo
{
    public interface IZaloGraphApiClient
    {
        Task<ZaloProfileDto> GetProfileAsync(string accessToken, CancellationToken cancellationToken = default);
    }
}
