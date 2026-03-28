using System.Text.Json;
using QLS.Backend.DTOs;
using QLS.Backend.Interfaces;

namespace QLS.Backend.Services;

public class WasherService : IWasherService
{
    private readonly HttpClient _httpClient;

    public WasherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WasherStatusDto> GetWasherStatusAsync(string deviceId)
    {
        var url = $"https://kic-laundry.lgthinq.com/status/{deviceId}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        // Header dùng để bypass auth từ frontend cung cấp
        request.Headers.Add("x-thinq-app-ver", "0.1");
        request.Headers.Add("x-thinq-client-type", "USER");
        request.Headers.Add("x-api-key", "vV6bStCpqr5Hqxbcr8Kmp9XkFh4VdlVp568YxBp5");
        request.Headers.Add("x-country-code", "VN");
        request.Headers.Add("x-client-id", "12345");
        request.Headers.Add("x-message-id", "9816091412");
        request.Headers.Add("x-service-code", "CHN000035");
        request.Headers.Add("x-service-phase", "OP");

        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Lỗi HTTP từ máy chủ LG: (Status {response.StatusCode})");
        }

        var jsonStr = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(jsonStr);
        var root = jsonDoc.RootElement;

        try 
        {
            var resultArr = root.GetProperty("result");
            if (resultArr.GetArrayLength() == 0)
                throw new Exception("Mảng result trống.");

            var snapshot = resultArr[0].GetProperty("snapshot");
            
            if (!snapshot.TryGetProperty("washerDryer", out var washerData))
                throw new Exception("Không tìm thấy thuốc tính washerDryer.");

            var curState = washerData.TryGetProperty("CurState", out var stateProp) && stateProp.ValueKind == JsonValueKind.String 
                ? stateProp.GetString() : "UNKNOWN";
                
            var courseNum = washerData.TryGetProperty("CourseNum", out var courseProp) && courseProp.ValueKind == JsonValueKind.String 
                ? courseProp.GetString() : "--";
                
            var remainHour = washerData.TryGetProperty("RemainHour", out var rhProp) && rhProp.ValueKind == JsonValueKind.Number 
                ? rhProp.GetInt32() : 0;
                
            var remainMin = washerData.TryGetProperty("RemainMin", out var rmProp) && rmProp.ValueKind == JsonValueKind.Number 
                ? rmProp.GetInt32() : 0;

            string timeString = "";
            if (remainHour > 0) timeString += $"{remainHour}h ";
            if (remainMin > 0) timeString += $"{remainMin}m";
            if (remainHour == 0 && remainMin == 0) timeString = "--";

            return new WasherStatusDto
            {
                CurState = curState ?? "UNKNOWN",
                CourseNum = courseNum ?? "--",
                RemainHour = remainHour,
                RemainMin = remainMin,
                TimeString = timeString.Trim()
            };
        }
        catch (Exception ex)
        {
            throw new Exception("Dữ liệu JSON từ LG không đúng cấu trúc dự kiến: " + ex.Message, ex);
        }
    }
}
