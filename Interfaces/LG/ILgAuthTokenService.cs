using QLS.Backend.DTOs.Lg;

namespace QLS.Backend.Interfaces.LG
{
    /// <summary>
    /// Interface cho service thực hiện toàn bộ luồng lấy OAuth Token từ LG API.
    /// </summary>
    public interface ILgAuthTokenService
    {
        /// <summary>
        /// Thực hiện toàn bộ 6-bước flow xác thực LG để lấy access_token.
        /// </summary>
        /// <param name="request">Thông tin email, password và quốc gia</param>
        /// <returns>Access token, refresh token và thông tin user</returns>
        Task<LgAuthTokenResult> GetAccessTokenAsync(LgLoginRequest request);

        /// <summary>
        /// Hash mật khẩu theo chuẩn SHA512 (lowercase hex) đúng format LG yêu cầu.
        /// </summary>
        string HashPassword(string password);

        /// <summary>
        /// Làm mới Access Token bằng Refresh Token (không cần login lại).
        /// </summary>
        /// <param name="refreshToken">Refresh Token hiện tại</param>
        /// <param name="oauth2BackendUrl">URL backend OAuth2 (vd: https://kr.biz.lgeapi.com/)</param>
        /// <returns>Access token mới và thời gian hết hạn</returns>
        Task<LgAuthTokenResult> RefreshAccessTokenAsync(string refreshToken, string oauth2BackendUrl);
    }
}
