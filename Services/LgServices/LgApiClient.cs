using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using QLS.Backend.DTOs.Lg;

namespace QLS.Backend.Services.LgService;

public class LgApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public LgApiClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<string> GetRawStatusAsync(string storeId, string userNo, string accessToken)
    {
        var section = _config.GetSection("LgApi");
        var url = $"{section["BaseUrl"]}{storeId}";
        return await SendThinqRequestAsync(url, userNo, accessToken);
    }

    /// <summary>
    /// Gọi LG API lấy setting của một device cụ thể: GET /devices/{deviceId}/settings
    /// </summary>
    public async Task<string> GetRawSettingsAsync(string deviceId, string userNo, string accessToken)
    {
        var url = $"https://kic-laundry.lgthinq.com/devices/{deviceId}/settings";
        return await SendThinqRequestAsync(url, userNo, accessToken);
    }

    /// <summary>
    /// Gửi setting lên LG API: POST /devices/{deviceId}/settings
    /// </summary>
    public async Task<string> UpdateSettingsAsync(string deviceId, object payload, string userNo, string accessToken)
    {
        var url = $"https://kic-laundry.lgthinq.com/devices/{deviceId}/settings";
        var json = JsonSerializer.Serialize(new { request = payload });
        return await SendThinqRequestAsync(url, userNo, accessToken, HttpMethod.Post, json);
    }

    public async Task<string> GetStoresAsync(string userNo, string accessToken)
    {
        var url = "https://kic-laundry.lgthinq.com/stores/page?page=1&pageSize=8";
        return await SendThinqRequestAsync(url, userNo, accessToken);
    }

    public async Task<LgStoreCreateResponse> CreateStoreLgAsync(LgStoreCreateRequest body, string userNo, string accessToken)
    {
        var url = "https://kic-laundry.lgthinq.com/stores";
        var json = JsonSerializer.Serialize(body);
        var responseString = await SendThinqRequestAsync(url, userNo, accessToken, HttpMethod.Post, json);
        return JsonSerializer.Deserialize<LgStoreCreateResponse>(responseString) ?? new();
    }

    private async Task<string> SendThinqRequestAsync(string url, string? userNo, string? accessToken, HttpMethod? method = null, string? body = null)
    {
        var section = _config.GetSection("LgApi");
        var requestMethod = method ?? HttpMethod.Get;
        var request = new HttpRequestMessage(requestMethod, url);

        if (body != null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }
        
        request.Headers.Add("x-thinq-app-ver", section["AppVer"] ?? "0.1");
        request.Headers.Add("x-thinq-client-type", section["ClientType"] ?? "USER");
        request.Headers.Add("x-api-key", section["ApiKey"]);
        request.Headers.Add("x-country-code", section["CountryCode"] ?? "VN");
        request.Headers.Add("x-client-id", section["ClientId"] ?? "12345");
        request.Headers.Add("x-service-code", "CHN000037"); // Sử dụng cấu hình cứng giống request mẫu thành công
        request.Headers.Add("x-service-phase", section["ServicePhase"] ?? "OP");
        request.Headers.Add("x-thinq-app-level", "DEV");
        request.Headers.Add("x-language-code", "en-VN");

        if (!string.IsNullOrEmpty(userNo))
            request.Headers.Add("x-user-no", userNo);
        
        if (!string.IsNullOrEmpty(accessToken))
            request.Headers.Add("x-emp-token", accessToken);

        request.Headers.Add("x-message-id", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }
}
