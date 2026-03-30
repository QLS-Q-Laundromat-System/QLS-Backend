using System.Text.Json;
using QLS.Backend.DTOs;
using QLS.Backend.Interfaces;

namespace QLS.Backend.Services;

public class WasherService : IWasherService
{
    private readonly HttpClient _httpClient;
    
    // Khởi tạo biến tĩnh lấy theo thời gian thực (Unix Timestamp - 10 chữ số).
    private static string _currentMessageId = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(); 

    public WasherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Tách riêng hàm gửi Request để dễ dàng gọi lại (Retry)
    private async Task<HttpResponseMessage> SendRequestAsync(string deviceId)
    {
        var url = $"https://kic-laundry.lgthinq.com/status/{deviceId}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        
        // Header dùng để bypass auth từ frontend cung cấp
        request.Headers.Add("x-thinq-app-ver", "0.1");
        request.Headers.Add("x-thinq-client-type", "USER");
        request.Headers.Add("x-api-key", "vV6bStCpqr5Hqxbcr8Kmp9XkFh4VdlVp568YxBp5");
        request.Headers.Add("x-country-code", "VN");
        request.Headers.Add("x-client-id", "12345");
        request.Headers.Add("x-message-id", _currentMessageId);
        request.Headers.Add("x-service-code", "CHN000035");
        request.Headers.Add("x-service-phase", "OP");

        return await _httpClient.SendAsync(request);
    }

    public async Task<List<WasherStatusDto>> GetWasherStatusAsync(string storeId)
    {
        var response = await SendRequestAsync(storeId);
        
        if (!response.IsSuccessStatusCode)
        {
            // Nếu bị lỗi, tiến hành làm mới Message ID chuẩn UNIX Time (10 chữ số thời gian)
            _currentMessageId = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            
            // Thử gọi lại API 1 lần nữa với Message ID mới
            response = await SendRequestAsync(storeId);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Lỗi HTTP từ máy chủ LG ngay cả sau khi thử lại: (Status {response.StatusCode})");
            }
        }

        var jsonStr = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(jsonStr);
        var root = jsonDoc.RootElement;

        try 
        {
            var resultArr = root.GetProperty("result");
            if (resultArr.GetArrayLength() == 0)
                throw new Exception("Mảng result trống.");

            var statusList = new List<WasherStatusDto>();

            foreach (var element in resultArr.EnumerateArray())
            {
                var deviceId = element.TryGetProperty("deviceId", out var deviceIdProp) && deviceIdProp.ValueKind == JsonValueKind.String 
                    ? deviceIdProp.GetString() : "UNKNOWN_DEVICE";

                var alias = element.TryGetProperty("alias", out var aliasProp) && aliasProp.ValueKind == JsonValueKind.String 
                    ? aliasProp.GetString() : "Máy giặt/sấy";

                if (!element.TryGetProperty("snapshot", out var snapshot)) continue;
                if (!snapshot.TryGetProperty("washerDryer", out var washerData)) continue;

                var curState = washerData.TryGetProperty("CurState", out var stateProp) && stateProp.ValueKind == JsonValueKind.String 
                    ? stateProp.GetString() : "UNKNOWN";
                    
                var course = washerData.TryGetProperty("Course", out var courseTextProp) && courseTextProp.ValueKind == JsonValueKind.String 
                    ? courseTextProp.GetString() : "";

                var courseNum = washerData.TryGetProperty("CourseNum", out var courseProp) && courseProp.ValueKind == JsonValueKind.String 
                    ? courseProp.GetString() : "--";
                    
                var remainHour = washerData.TryGetProperty("RemainHour", out var rhProp) && rhProp.ValueKind == JsonValueKind.Number 
                    ? rhProp.GetInt32() : 0;
                    
                var remainMin = washerData.TryGetProperty("RemainMin", out var rmProp) && rmProp.ValueKind == JsonValueKind.Number 
                    ? rmProp.GetInt32() : 0;

                var remainTime = washerData.TryGetProperty("RemainTime", out var rtProp) && rtProp.ValueKind == JsonValueKind.Number 
                    ? rtProp.GetInt32() : 0;

                string timeString = "";
                // Nếu có remainTime (thường cho máy sấy), ưu tiên sử dụng nó nếu hour/min bằng 0
                if (remainHour > 0 || remainMin > 0)
                {
                    if (remainHour > 0) timeString += $"{remainHour}h ";
                    if (remainMin > 0) timeString += $"{remainMin}m";
                }
                else if (remainTime > 0)
                {
                    timeString = $"{remainTime}m";
                }
                
                if (string.IsNullOrEmpty(timeString)) timeString = "--";

                statusList.Add(new WasherStatusDto
                {
                    DeviceId = deviceId ?? "UNKNOWN_DEVICE",
                    Alias = alias ?? "Máy giặt/sấy",
                    CurState = curState ?? "UNKNOWN",
                    Course = course ?? "",
                    CourseNum = courseNum ?? "--",
                    RemainHour = remainHour,
                    RemainMin = remainMin,
                    RemainTime = remainTime,
                    TimeString = timeString.Trim()
                });
            }

            return statusList;
        }
        catch (Exception ex)
        {
            throw new Exception("Dữ liệu JSON từ LG không đúng cấu trúc dự kiến: " + ex.Message, ex);
        }
    }
}
