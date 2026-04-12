using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using QLS.Backend.DTOs.Lg;
using QLS.Backend.Interfaces.LG;

namespace QLS.Backend.Services.LgServices.authToken
{
    /// <summary>
    /// Triển khai 7-bước flow xác thực LG để lấy OAuth access_token.
    ///
    /// Luồng:
    ///   Bước 1 → POST empb2b/v1.0/account/session/{email}     (EMP B2B - VN)
    ///              SHA512(password) → nhận loginSessionID
    ///   Bước 2 → GET  oauth/1.0/datetime                      (OAuth VN)
    ///              Nhận serverDate để ký request
    ///   Bước 3 → POST kr.m.biz.lgaccount.com/oauthSignature   (LG Proxy)
    ///              Gửi text = path + "\n" + date → nhận HMAC-SHA1 signature
    ///   Bước 4 → GET  oauth/1.0/emp/oauth2/auth               (OAuth VN)
    ///              Signature headers → nhận code + user_number + oauth2_backend_url
    ///   Bước 5 → GET  oauth/1.0/datetime                      (OAuth KR)
    ///              Nhận serverDate mới để ký token request
    ///   Bước 6 → HMAC-SHA1 nội bộ với CLIENT_SECRET
    ///              Tính signature cho token request
    ///   Bước 7 → POST oauth/1.0/oauth2/token                  (OAuth KR)
    ///              code + signature → nhận access_token + refresh_token
    /// </summary>
    public class LgAuthTokenService : ILgAuthTokenService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LgAuthTokenService> _logger;
        private readonly IConfiguration _configuration;

        // ─── Hằng số cấu hình LG (phân tích từ app.6677082b.js + network captures) ─
        private const string EmpB2BBaseUrl       = "https://vn.emp.biz.lgeapi.com";
        private const string OAuthVnBaseUrl       = "https://vn.biz.lgeapi.com";
        private const string OAuthSignatureUrl    = "https://kr.m.biz.lgaccount.com/oauthSignature";

        private const string AppKeyVn             = "04828ef6a030455885d3c4604e3b1623";  // EMP B2B + VN OAuth
        private const string ClientId             = "ed360dc33b4741bd995cb9474ce07c80";  // OAuth2 client_id (KR)
        private const string ClientSecret         = "b79f2f4c46bd48e9ab99a79699fdd400";  // HMAC-SHA1 key (từ app.js)
        private const string BizCode              = "B01004C037";

        private const string RedirectUri          = "http://kic-laundry-web.lgthinq.com/redirect";
        private const string TokenUrlPath         = "/oauth/1.0/oauth2/token";
        private const string OAuthState           = "login";

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull
        };

        public LgAuthTokenService(
            HttpClient httpClient,
            ILogger<LgAuthTokenService> logger,
            IConfiguration configuration)
        {
            _httpClient    = httpClient;
            _logger        = logger;
            _configuration = configuration;
        }

        // ════════════════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ════════════════════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<LgAuthTokenResult> GetAccessTokenAsync(LgLoginRequest request)
        {
            var country = request.Country.ToUpper();

            _logger.LogInformation("[LG Auth] Bắt đầu flow xác thực cho {Email}", request.Email);

            // ── Bước 1: Đăng nhập EMP B2B → lấy loginSessionID ──────────────
            var loginSessionId = await Step1_LoginAsync(request.Email, request.Password, country);
            _logger.LogDebug("[LG Bước 1] ✓ loginSessionID: {Id}", loginSessionId[..Math.Min(20, loginSessionId.Length)] + "...");

            // ── Bước 2: Lấy server datetime từ OAuth VN ──────────────────────
            var serverDate1 = await Step2_GetDateTimeAsync(OAuthVnBaseUrl, AppKeyVn);
            _logger.LogDebug("[LG Bước 2] ✓ serverDate: {Date}", serverDate1);

            // ── Bước 3: Gọi /oauthSignature endpoint để lấy SHA1 signature ───
            var encodedRedirect = Uri.EscapeDataString(RedirectUri);
            var authPath        = $"/oauth/1.0/emp/oauth2/auth?client_id={ClientId}&country_code={country}&redirect_uri={encodedRedirect}&response_type=code&state={OAuthState}";
            var signText1       = $"{authPath}\n{serverDate1}";
            var signature1      = await Step3_GetSignatureFromEndpointAsync(signText1, BizCode);
            _logger.LogDebug("[LG Bước 3] ✓ Signature: {Sig}", signature1);

            // ── Bước 4: Lấy OAuth2 code ──────────────────────────────────────
            var (authCode, userNumber, oauth2BackendUrl) = await Step4_GetOAuth2CodeAsync(
                country, loginSessionId, serverDate1, signature1, encodedRedirect);
            _logger.LogDebug("[LG Bước 4] ✓ Code: {Code} | UserNo: {No}", authCode, userNumber);

            // ── Bước 5: Lấy datetime từ OAuth KR ─────────────────────────────
            // oauth2BackendUrl có trailing slash (vd: "https://kr.biz.lgeapi.com/")
            var krBaseUrl   = oauth2BackendUrl.TrimEnd('/'); // bỏ trailing slash để nối path đúng
            var serverDate2 = await Step5_GetDateTimeKrAsync(krBaseUrl);
            _logger.LogDebug("[LG Bước 5] ✓ KR Date: {Date}", serverDate2);

            // ── Bước 6: Tính HMAC-SHA1 cho token request (dùng CLIENT_SECRET) ─
            var redirectQs   = Uri.EscapeDataString(RedirectUri);
            var tokenParams  = $"code={Uri.EscapeDataString(authCode)}&grant_type=authorization_code&redirect_uri={redirectQs}";
            var signText2    = $"{TokenUrlPath}?{tokenParams}\n{serverDate2}";
            var signature2   = Step6_HmacSha1Sign(signText2, ClientSecret);
            _logger.LogDebug("[LG Bước 6] ✓ Token Signature: {Sig}", signature2);

            // ── Bước 7: Đổi code lấy access_token ───────────────────────────
            var tokenResult = await Step7_ExchangeTokenAsync(
                krBaseUrl, tokenParams, serverDate2, signature2);
            _logger.LogInformation("[LG Auth] ✅ Lấy access_token thành công cho {Email}", request.Email);

            return new LgAuthTokenResult
            {
                AccessToken      = tokenResult.AccessToken,
                RefreshToken     = tokenResult.RefreshToken,
                UserNo           = userNumber,
                UserId           = request.Email,
                UserCountry      = country,
                Oauth2BackendUrl = oauth2BackendUrl,
                ExpiresIn        = int.TryParse(tokenResult.ExpiresIn, out var exp) ? exp : 3600
            };
        }

        /// <inheritdoc/>
        public string HashPassword(string password)
        {
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash  = SHA512.HashData(bytes);
            return Convert.ToHexString(hash).ToLower();
        }

        /// <inheritdoc/>
        public async Task<LgAuthTokenResult> RefreshAccessTokenAsync(string refreshToken, string oauth2BackendUrl)
        {
            _logger.LogInformation("[LG Auth] Đang làm mới access token bằng refresh token...");

            // 1. Lấy datetime từ OAuth KR
            var krBaseUrl   = oauth2BackendUrl.TrimEnd('/');
            var serverDate = await Step5_GetDateTimeKrAsync(krBaseUrl);

            // 2. Chuẩn bị tham số
            var refreshParams = $"grant_type=refresh_token&refresh_token={Uri.EscapeDataString(refreshToken)}";

            // 3. Ký request (HMAC-SHA1 với ClientSecret)
            // Path: /oauth/1.0/oauth2/token?{params}
            var signText = $"{TokenUrlPath}?{refreshParams}\n{serverDate}";
            var signature = Step6_HmacSha1Sign(signText, ClientSecret);

            // 4. Gửi request lấy token mới
            var tokenResult = await Step7_ExchangeTokenAsync(krBaseUrl, refreshParams, serverDate, signature);

            _logger.LogInformation("[LG Auth] ✅ Làm mới access token thành công.");

            return new LgAuthTokenResult
            {
                AccessToken      = tokenResult.AccessToken,
                RefreshToken     = string.IsNullOrEmpty(tokenResult.RefreshToken) ? refreshToken : tokenResult.RefreshToken,
                ExpiresIn        = int.TryParse(tokenResult.ExpiresIn, out var exp) ? exp : 3600,
                Oauth2BackendUrl = oauth2BackendUrl
            };
        }

        // ════════════════════════════════════════════════════════════════════════
        // PRIVATE STEPS
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Bước 1: POST /empb2b/v1.0/account/session/{email}
        /// Gửi SHA512(password) → nhận loginSessionID.
        /// </summary>
        private async Task<string> Step1_LoginAsync(string email, string password, string country)
        {
            var encodedEmail = Uri.EscapeDataString(email);
            var url          = $"{EmpB2BBaseUrl}/empb2b/v1.0/account/session/{encodedEmail}";
            var userAuth     = HashPassword(password);

            var form = new Dictionary<string, string>
            {
                ["otp_use_yn"] = "N",
                ["target_url"] = $"https://vn.m.biz.lgaccount.com/email/account_protect_auth?biz_code={BizCode}.01&country={country}&language=en-{country}",
                ["user_auth"]  = userAuth
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(form)
            };
            req.Headers.TryAddWithoutValidation("X-Device-Language-Type", "IETF");
            req.Headers.TryAddWithoutValidation("X-Device-Publish-Flag",  "Y");
            req.Headers.TryAddWithoutValidation("X-Device-Country",       country);
            req.Headers.TryAddWithoutValidation("X-Lge-AppKey",           AppKeyVn);
            req.Headers.TryAddWithoutValidation("X-Device-Language",      $"en-{country}");
            req.Headers.TryAddWithoutValidation("Accept",                  "application/json");

            using var res  = await _httpClient.SendAsync(req);
            var body       = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"[LG Bước 1] Đăng nhập thất bại ({(int)res.StatusCode}): {body}");

            using var doc = JsonDocument.Parse(body);
            var sessionId = doc.RootElement
                .GetProperty("account")
                .GetProperty("loginSessionID")
                .GetString();

            if (string.IsNullOrEmpty(sessionId))
                throw new InvalidOperationException("[LG Bước 1] loginSessionID rỗng – sai email/password?");

            return sessionId;
        }

        /// <summary>
        /// Bước 2 + 5: GET /oauth/1.0/datetime — lấy server date string.
        /// </summary>
        private async Task<string> Step2_GetDateTimeAsync(string baseUrl, string appKey)
        {
            var url = $"{baseUrl}/oauth/1.0/datetime?_={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("Accept",       "application/json");
            req.Headers.TryAddWithoutValidation("x-lge-appkey", appKey);

            using var res = await _httpClient.SendAsync(req);
            var body     = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"[LG Bước 2] Lấy datetime thất bại ({(int)res.StatusCode}): {body}");

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("date").GetString()
                   ?? throw new InvalidOperationException("[LG Bước 2] Không có trường 'date' trong response");
        }

        /// <summary>
        /// Bước 5: Lấy datetime từ KR backend (dùng ClientId thay AppKeyVn).
        /// </summary>
        private async Task<string> Step5_GetDateTimeKrAsync(string krBaseUrl)
        {
            var url = $"{krBaseUrl}/oauth/1.0/datetime";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("Accept",        "application/json");
            req.Headers.TryAddWithoutValidation("x-lge-appKey",  ClientId);
            req.Headers.TryAddWithoutValidation("x-message-id",  GenerateMessageId());

            using var res = await _httpClient.SendAsync(req);
            var body     = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"[LG Bước 5] Lấy datetime KR thất bại ({(int)res.StatusCode}): {body}");

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("date").GetString()
                   ?? throw new InvalidOperationException("[LG Bước 5] Không có trường 'date' trong response");
        }

        /// <summary>
        /// Bước 3: Gọi endpoint /oauthSignature của LG để lấy HMAC-SHA1 signature.
        /// LG server nắm giữ secret key, ta chỉ gửi text cần ký.
        /// </summary>
        private async Task<string> Step3_GetSignatureFromEndpointAsync(string signText, string bizCode)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, OAuthSignatureUrl);
            req.Headers.TryAddWithoutValidation("Accept", "text/plain, */*; q=0.01");
            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["biz_code"]    = bizCode,
                ["server_type"] = "OP",
                ["text"]        = signText
            });

            using var res = await _httpClient.SendAsync(req);
            var body     = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"[LG Bước 3] /oauthSignature thất bại ({(int)res.StatusCode}): {body}");

            if (string.IsNullOrWhiteSpace(body))
                throw new InvalidOperationException("[LG Bước 3] /oauthSignature trả về rỗng");

            return body.Trim();
        }

        /// <summary>
        /// Bước 4: GET /oauth/1.0/emp/oauth2/auth
        /// Gửi signature headers → nhận redirect_uri chứa code + user_number.
        /// </summary>
        private async Task<(string code, string userNumber, string oauth2BackendUrl)>
            Step4_GetOAuth2CodeAsync(
                string country,
                string loginSessionId,
                string serverDate,
                string signature,
                string encodedRedirect)
        {
            var ts  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var url = $"{OAuthVnBaseUrl}/oauth/1.0/emp/oauth2/auth?client_id={ClientId}&country_code={country}&redirect_uri={encodedRedirect}&response_type=code&state={OAuthState}&_={ts}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("Accept",                 "application/json");
            req.Headers.TryAddWithoutValidation("Content-Type",           "application/x-www-form-urlencoded");
            req.Headers.TryAddWithoutValidation("x-lge-appkey",           AppKeyVn);
            req.Headers.TryAddWithoutValidation("X-Login-Session",        loginSessionId);
            req.Headers.TryAddWithoutValidation("x-lge-oauth-date",       serverDate);
            req.Headers.TryAddWithoutValidation("x-lge-oauth-signature",  signature);

            using var res = await _httpClient.SendAsync(req);
            var body     = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"[LG Bước 4] Lấy OAuth2 code thất bại ({(int)res.StatusCode}): {body}");

            using var doc       = JsonDocument.Parse(body);
            var rawRedir        = doc.RootElement.GetProperty("redirect_uri").GetString()
                                  ?? throw new InvalidOperationException("[LG Bước 4] Thiếu redirect_uri");

            // redirect_uri bị double-encode: decode 1 lần
            var decodedRedir    = Uri.UnescapeDataString(rawRedir);
            var redirectUri     = new Uri(decodedRedir);
            var qs              = HttpUtility.ParseQueryString(redirectUri.Query);

            var code             = qs["code"]             ?? throw new InvalidOperationException("[LG Bước 4] Thiếu code");
            var userNumber       = qs["user_number"]      ?? throw new InvalidOperationException("[LG Bước 4] Thiếu user_number");
            var oauth2BackendUrl = qs["oauth2_backend_url"] ?? throw new InvalidOperationException("[LG Bước 4] Thiếu oauth2_backend_url");

            return (code, userNumber, oauth2BackendUrl);
        }

        /// <summary>
        /// Bước 6: Tính HMAC-SHA1 nội bộ với CLIENT_SECRET (từ app.js của kic-laundry).
        /// Dùng cho token request (KR backend), khác với Bước 3 dùng LG endpoint.
        /// </summary>
        private static string Step6_HmacSha1Sign(string text, string secret)
        {
            using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(secret));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }

        /// <summary>
        /// Bước 7: POST /oauth/1.0/oauth2/token
        /// Đổi authorization code lấy access_token + refresh_token.
        /// </summary>
        private async Task<LgTokenResponse> Step7_ExchangeTokenAsync(
            string krBaseUrl,
            string tokenParams,
            string serverDate,
            string signature)
        {
            // URL: krBaseUrl + /oauth/1.0/oauth2/token?{params}
            var url = $"{krBaseUrl}{TokenUrlPath}?{tokenParams}";

            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                // Body rỗng — params đã nằm trong query string
                Content = new StringContent("", Encoding.UTF8, "application/x-www-form-urlencoded")
            };
            req.Headers.TryAddWithoutValidation("Accept",                 "application/json");
            req.Headers.TryAddWithoutValidation("x-lge-appKey",           ClientId);
            req.Headers.TryAddWithoutValidation("x-lge-oauth-date",       serverDate);
            req.Headers.TryAddWithoutValidation("x-lge-oauth-signature",  signature);
            req.Headers.TryAddWithoutValidation("x-message-id",           GenerateMessageId());

            using var res = await _httpClient.SendAsync(req);
            var body     = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"[LG Bước 7] Lấy access_token thất bại ({(int)res.StatusCode}): {body}");

            using var doc  = JsonDocument.Parse(body);
            var root       = doc.RootElement;

            var accessToken  = root.GetProperty("access_token").GetString()
                               ?? throw new InvalidOperationException("[LG Bước 7] access_token rỗng");
            var refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : "";
            var expiresIn    = root.TryGetProperty("expires_in",    out var ei) ? ei.GetString() ?? "3600" : "3600";
            var backendUrl   = root.TryGetProperty("oauth2_backend_url", out var bu) ? bu.GetString() ?? "" : "";

            return new LgTokenResponse
            {
                AccessToken      = accessToken,
                RefreshToken     = refreshToken,
                ExpiresIn        = expiresIn,
                Oauth2BackendUrl = backendUrl
            };
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static string GenerateMessageId()
            => Random.Shared.NextInt64(1_000_000_000L, 9_999_999_999L).ToString();
    }
}
