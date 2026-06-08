using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QLS.Backend.DTOs.Zalo;
using QLS.Backend.Exceptions;
using QLS.Backend.Interfaces.Zalo;

namespace QLS.Backend.Services.Zalo
{
    public class ZaloGraphApiClient : IZaloGraphApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ZaloGraphApiClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<ZaloProfileDto> GetProfileAsync(
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            var appSecretKey = _configuration["Zalo:AppSecretKey"];
            if (string.IsNullOrWhiteSpace(appSecretKey))
            {
                throw new ApiException("Thiếu cấu hình Zalo App Secret Key.", 500);
            }

            var graphApiUrl = _configuration["Zalo:GraphApiUrl"];
            if (string.IsNullOrWhiteSpace(graphApiUrl))
            {
                graphApiUrl = "https://graph.zalo.me/v2.0/me";
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{graphApiUrl}?fields=id,name,picture");
            request.Headers.TryAddWithoutValidation("access_token", accessToken);
            request.Headers.TryAddWithoutValidation("appsecret_proof", CalculateAppSecretProof(accessToken, appSecretKey));

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException)
            {
                throw new ApiException("Không thể kết nối Zalo Graph API.", 502);
            }

            using (response)
            {
                ZaloProfileDto? profile;
                try
                {
                    profile = await response.Content.ReadFromJsonAsync<ZaloProfileDto>(cancellationToken: cancellationToken);
                }
                catch (JsonException)
                {
                    throw new ApiException("Zalo Graph API trả về dữ liệu không hợp lệ.", 502);
                }

                if (profile != null && profile.Error != 0)
                {
                    throw new ApiException(ZaloErrors.GetFriendlyMessage(profile.Error, profile.Message), 401);
                }

                if (!response.IsSuccessStatusCode || profile == null || string.IsNullOrWhiteSpace(profile.Id))
                {
                    throw new ApiException("Access token Zalo không hợp lệ hoặc đã hết hạn.", 401);
                }

                return profile;
            }
        }

        private static string CalculateAppSecretProof(string accessToken, string appSecretKey)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecretKey));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(accessToken))).ToLowerInvariant();
        }
    }
}
