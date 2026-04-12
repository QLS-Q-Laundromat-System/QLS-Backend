using System.Text.Json;
using QLS.Backend.DTOs;

namespace QLS.Backend.Services.LgService;

public static class LgMapper
{
    public static List<MachineDetailDto> MapToDto(string jsonStr)
    {
        var statusList = new List<MachineDetailDto>();
        using var doc = JsonDocument.Parse(jsonStr);
        
        if (!doc.RootElement.TryGetProperty("result", out var resultArr)) return statusList;

        foreach (var element in resultArr.EnumerateArray())
        {
            var deviceId = element.GetProperty("deviceId").GetString() ?? "";
            var alias = element.GetProperty("alias").GetString() ?? "Machine";
            var typeRaw = element.GetProperty("deviceType").GetString() ?? "211";

            if (!element.TryGetProperty("snapshot", out var snapshot)) continue;
            if (!snapshot.TryGetProperty("washerDryer", out var wd)) continue;

            // Xử lý Trạng thái & Thời gian
            string mappedDeviceType = typeRaw == "211" ? "0" : "1";
            string? curState = null;
            string? course = null;
            string? courseNum = null;
            int? remainHour = null;
            int? remainMin = null;
            int? remainTime = null;
            string? process = null;

            if (mappedDeviceType == "0")
            {
                curState = wd.TryGetProperty("CurState", out var s) ? (s.GetString() ?? "INITIAL") : "INITIAL";
                remainHour = wd.TryGetProperty("RemainHour", out var rh) ? (rh.ValueKind == JsonValueKind.Number ? rh.GetInt32() : 0) : 0;
                remainMin = wd.TryGetProperty("RemainMin", out var rm) ? (rm.ValueKind == JsonValueKind.Number ? rm.GetInt32() : 0) : 0;
                courseNum = wd.TryGetProperty("CourseNum", out var c) ? (c.GetString() ?? "") : 
                         (wd.TryGetProperty("courseNum", out var c2) ? (c2.GetString() ?? "") : "");
            }
            else
            {
                course = wd.TryGetProperty("Course", out var c) ? (c.GetString() ?? "") : 
                         (wd.TryGetProperty("course", out var c2) ? (c2.GetString() ?? "") : "");
                remainTime = wd.TryGetProperty("RemainTime", out var rt) ? (rt.ValueKind == JsonValueKind.Number ? rt.GetInt32() : 0) : 
                             (wd.TryGetProperty("remainTime", out var rt2) ? (rt2.ValueKind == JsonValueKind.Number ? rt2.GetInt32() : 0) : 0);
                process = wd.TryGetProperty("Process", out var p) ? (p.GetString() ?? "") : 
                          (wd.TryGetProperty("process", out var p2) ? (p2.GetString() ?? "") : "");
            }

            bool isOnline = true;
            if (element.TryGetProperty("online", out var onlineElement) && (onlineElement.ValueKind == JsonValueKind.True || onlineElement.ValueKind == JsonValueKind.False)) 
                isOnline = onlineElement.GetBoolean();
            else if (snapshot.TryGetProperty("online", out var snapshotOnline) && (snapshotOnline.ValueKind == JsonValueKind.True || snapshotOnline.ValueKind == JsonValueKind.False)) 
                isOnline = snapshotOnline.GetBoolean();

            statusList.Add(new MachineDetailDto {
                DeviceId = deviceId,
                DeviceType = mappedDeviceType,
                Alias = alias,
                CurState = curState,
                Course = course,
                CourseNum = courseNum,
                RemainHour = remainHour,
                RemainMin = remainMin,
                RemainTime = remainTime,
                Process = process,
                Online = isOnline
            });
        }
        return statusList;
    }
}
