using System.Text.Json;
using QLS.Backend.DTOs;
using QLS.Backend.Interfaces;
using QLS.Backend.Data;
using QLS.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace QLS.Backend.Services;

public class WasherService : IWasherService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    
    // Khởi tạo biến tĩnh lấy theo thời gian thực (Unix Timestamp - 10 chữ số).
    private static string _currentMessageId = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(); 

    public WasherService(HttpClient httpClient, AppDbContext context)
    {
        _httpClient = httpClient;
        _context = context;
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

            // Lấy danh sách MachineId đã tồn tại trong Store này để kiểm tra nhanh trong bộ nhớ
            var existingMachineIds = await _context.Machines
                .Where(m => m.StoreId == storeId)
                .Select(m => m.MachineId)
                .ToHashSetAsync();

            var statusList = new List<WasherStatusDto>();
            var newMachines = new List<Machine>();

            foreach (var element in resultArr.EnumerateArray())
            {
                var deviceId = element.TryGetProperty("deviceId", out var deviceIdProp) && deviceIdProp.ValueKind == JsonValueKind.String 
                    ? deviceIdProp.GetString() : "UNKNOWN_DEVICE";

                var alias = element.TryGetProperty("alias", out var aliasProp) && aliasProp.ValueKind == JsonValueKind.String 
                    ? aliasProp.GetString() : "Máy giặt/sấy";

                if (!element.TryGetProperty("snapshot", out var snapshot)) continue;
                
                // Thử lấy dữ liệu từ 'washerDryer' hoặc 'dryer' (tùy theo loại máy)
                JsonElement washerData;
                if (snapshot.TryGetProperty("washerDryer", out var wdData)) 
                {
                    washerData = wdData;
                }
                else if (snapshot.TryGetProperty("dryer", out var dData))
                {
                    washerData = dData;
                }
                else 
                {
                    continue; // Không tìm thấy dữ liệu trạng thái
                }

                // Trích xuất trạng thái hiện tại (CurState cho máy giặt, process cho máy sấy)
                string curState = "UNKNOWN";
                if (washerData.TryGetProperty("CurState", out var stateProp) && stateProp.ValueKind == JsonValueKind.String)
                {
                    curState = stateProp.GetString() ?? "UNKNOWN";
                }
                else if (washerData.TryGetProperty("process", out var processProp) && processProp.ValueKind == JsonValueKind.String)
                {
                    curState = processProp.GetString() ?? "UNKNOWN";
                }
                    
                // Cố gắng lấy tên chương trình (thử cả viết hoa và viết thường)
                string course = "";
                if (washerData.TryGetProperty("Course", out var cTextProp) && cTextProp.ValueKind == JsonValueKind.String)
                {
                    course = cTextProp.GetString() ?? "";
                }
                else if (washerData.TryGetProperty("course", out var cLowerProp) && cLowerProp.ValueKind == JsonValueKind.String)
                {
                    course = cLowerProp.GetString() ?? "";
                }

                // Cố gắng lấy mã chương trình (thử cả CourseNum và các biến thể)
                string courseNum = "--";
                if (washerData.TryGetProperty("CourseNum", out var cNumProp) && cNumProp.ValueKind == JsonValueKind.String)
                {
                    courseNum = cNumProp.GetString() ?? "--";
                }
                else if (washerData.TryGetProperty("courseNum", out var cNumLowerProp) && cNumLowerProp.ValueKind == JsonValueKind.String)
                {
                    courseNum = cNumLowerProp.GetString() ?? "--";
                }
                    
                var remainHour = washerData.TryGetProperty("RemainHour", out var rhProp) && rhProp.ValueKind == JsonValueKind.Number 
                    ? rhProp.GetInt32() : 0;
                    
                var remainMin = washerData.TryGetProperty("RemainMin", out var rmProp) && rmProp.ValueKind == JsonValueKind.Number 
                    ? rmProp.GetInt32() : 0;

                // Thử lấy RemainTime (viết hoa) hoặc remainTime (viết thường)
                int remainTime = 0;
                if (washerData.TryGetProperty("RemainTime", out var rtProp) && rtProp.ValueKind == JsonValueKind.Number)
                {
                    remainTime = rtProp.GetInt32();
                }
                else if (washerData.TryGetProperty("remainTime", out var rtLowerProp) && rtLowerProp.ValueKind == JsonValueKind.Number)
                {
                    remainTime = rtLowerProp.GetInt32();
                }

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

                var online = snapshot.TryGetProperty("online", out var onlineProp) && 
                              (onlineProp.ValueKind == JsonValueKind.True || 
                               (onlineProp.ValueKind == JsonValueKind.String && onlineProp.GetString() == "true"));

                // Trích xuất deviceType (211: Giặt, 212: Sấy)
                var deviceTypeRaw = element.TryGetProperty("deviceType", out var typeProp) 
                    ? (typeProp.ValueKind == JsonValueKind.Number ? typeProp.GetInt32().ToString() : typeProp.GetString() ?? "0")
                    : "0";
                
                int deviceType = deviceTypeRaw == "211" ? 0 : (deviceTypeRaw == "212" ? 1 : 0);

                // Tự động lưu máy vào database nếu chưa tồn tại
                if (deviceId != "UNKNOWN_DEVICE" && !existingMachineIds.Contains(deviceId))
                {
                    newMachines.Add(new Machine
                    {
                        MachineId = deviceId!,
                        StoreId = storeId,
                        Type = deviceType.ToString(),
                        Capacity = "UNKNOWN"
                    });
                    existingMachineIds.Add(deviceId!); // Tránh trùng lặp trong cùng một lô kết quả
                }

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
                    TimeString = timeString.Trim(),
                    Online = online,
                    DeviceType = deviceType
                });
            }

            if (newMachines.Any())
            {
                _context.Machines.AddRange(newMachines);
                await _context.SaveChangesAsync();
            }

            return statusList;
        }
        catch (Exception ex)
        {
            throw new Exception("Dữ liệu JSON từ LG không đúng cấu trúc dự kiến: " + ex.Message, ex);
        }
    }
}
