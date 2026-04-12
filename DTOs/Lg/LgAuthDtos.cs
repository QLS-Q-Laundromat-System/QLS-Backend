namespace QLS.Backend.DTOs.Lg
{
    // ==================== REQUEST DTOs ====================

    /// <summary>
    /// Request để lấy LG OAuth Token.
    /// Người dùng chỉ cần cung cấp email và password tài khoản LG.
    /// Password sẽ được hash SHA512 tự động trước khi gửi đến LG API.
    /// </summary>
    public class LgLoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Country { get; set; } = "VN";
    }

    /// <summary>
    /// Request để làm mới Access Token từ LG.
    /// </summary>
    public class LgRefreshRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
        public string Oauth2BackendUrl { get; set; } = string.Empty;
    }

    // ==================== INTERMEDIATE DTOs ====================

    /// <summary>Response từ bước 1: POST empb2b/v1.0/account/session/{email}</summary>
    public class LgSessionResponse
    {
        public LgAccountInfo? Account { get; set; }
    }

    public class LgAccountInfo
    {
        public string LoginSessionID { get; set; } = string.Empty;
        public string UserID { get; set; } = string.Empty;
        public string UserIDType { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
        public string Age { get; set; } = string.Empty;
        public LgUsrRoleInfo? UsrRoleInfo { get; set; }
    }

    public class LgUsrRoleInfo
    {
        public string PendApprYn { get; set; } = string.Empty;
        public string PendApprRoleName { get; set; } = string.Empty;
        public string LastRoleName { get; set; } = string.Empty;
    }

    /// <summary>Response từ bước 2: GET oauth/1.0/datetime</summary>
    public class LgDateTimeResponse
    {
        public string Date { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }

    /// <summary>Response từ bước 4: GET oauth/1.0/emp/oauth2/auth</summary>
    public class LgOAuth2AuthResponse
    {
        public string RedirectUri { get; set; } = string.Empty;
    }

    /// <summary>Response từ bước 6: POST oauth/1.0/oauth2/token</summary>
    public class LgTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string ExpiresIn { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string Oauth2BackendUrl { get; set; } = string.Empty;
    }

    // ==================== FINAL RESPONSE DTO ====================

    /// <summary>
    /// Kết quả trả về sau khi hoàn tất toàn bộ flow xác thực LG.
    /// </summary>
    public class LgAuthTokenResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string UserNo { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserCountry { get; set; } = string.Empty;
        public string Oauth2BackendUrl { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }
}
