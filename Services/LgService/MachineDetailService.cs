using System.Text.Json;
using QLS.Backend.DTOs;

namespace QLS.Backend.Services;

public interface IMachineDetailService {
    Task<List<MachineDetailDto>> GetLgMachineStatusAsync();
}

public class MachineDetailService : IMachineDetailService
{
    private readonly HttpClient _httpClient;
    private readonly string _lgApiUrl = "https://kic-laundry.lgthinq.com/status/c7863f0d53ac48bfb04d4b1367e664b7";

    public MachineDetailService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<MachineDetailDto>> GetLgMachineStatusAsync()
    {
        var statusList = new List<MachineDetailDto>();

        var request = new HttpRequestMessage(HttpMethod.Get, _lgApiUrl);

        // Thêm các Headers như Sơn yêu cầu
        request.Headers.Add("x-thinq-app-ver", "0.1");
        request.Headers.Add("x-thinq-client-type", "USER");
        request.Headers.Add("x-api-key", "vV6bStCpqr5Hqxbcr8Kmp9XkFh4VdlVp568YxBp5");
        request.Headers.Add("x-country-code", "VN");
        request.Headers.Add("x-client-id", "12345");
        request.Headers.Add("x-message-id", Guid.NewGuid().ToString()); // Tạo ID ngẫu nhiên mỗi lần gọi
        request.Headers.Add("x-service-code", "CHN000035");
        request.Headers.Add("x-service-phase", "OP");

        try {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            // Giả định cấu trúc JSON trả về có mảng thiết bị (Sơn cần điều chỉnh path này cho khớp JSON thật của LG)
            if (root.TryGetProperty("data", out var dataElement)) {
                foreach (var item in dataElement.EnumerateArray()) {
                    statusList.Add(new MachineDetailDto {
                        DeviceId = item.GetProperty("deviceId").GetString() ?? "UNKNOWN",
                        Alias = item.GetProperty("alias").GetString() ?? "Máy giặt/sấy",
                        CurState = item.GetProperty("curState").GetString() ?? "UNKNOWN",
                        Course = item.GetProperty("course").GetString() ?? "",
                        CourseNum = item.GetProperty("courseNum").GetString() ?? "--",
                        RemainHour = item.TryGetProperty("remainHour", out var rh) ? rh.GetInt32() : 0,
                        RemainMin = item.TryGetProperty("remainMin", out var rm) ? rm.GetInt32() : 0,
                        RemainTime = item.TryGetProperty("remainTime", out var rt) ? rt.GetInt32() : 0,
                        TimeString = item.GetProperty("timeString").GetString()?.Trim() ?? "",
                        Online = item.GetProperty("online").GetBoolean(),
                        DeviceType = item.GetProperty("deviceType").GetString() ?? ""
                    });
                }
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"Lỗi gọi API LG: {ex.Message}");
        }

        return statusList;
    }
}