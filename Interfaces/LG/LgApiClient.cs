using System.Net.Http.Headers;

namespace QLS.Backend.Integrations.LG;

public class LgApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public LgApiClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<string> GetRawStatusAsync(string storeId)
    {
        var section = _config.GetSection("LgApi");
        var url = $"{section["BaseUrl"]}{storeId}";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        
        // Headers lấy từ appsettings.json
        request.Headers.Add("x-thinq-app-ver", section["AppVer"]);
        request.Headers.Add("x-thinq-client-type", section["ClientType"]);
        request.Headers.Add("x-api-key", section["ApiKey"]);
        request.Headers.Add("x-country-code", section["CountryCode"]);
        request.Headers.Add("x-client-id", section["ClientId"]);
        request.Headers.Add("x-service-code", section["ServiceCode"]);
        request.Headers.Add("x-service-phase", section["ServicePhase"]);
        
        // Message ID số và duy nhất cho mỗi lần gọi
        request.Headers.Add("x-message-id", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        request.Headers.Add("User-Agent", "PostmanRuntime/7.32.3");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }
}