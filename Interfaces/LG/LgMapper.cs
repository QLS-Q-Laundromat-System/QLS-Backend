using System.Text.Json;
using QLS.Backend.DTOs;

namespace QLS.Backend.Integrations.LG;

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
            string state = typeRaw == "211" ? 
                (wd.TryGetProperty("CurState", out var s) ? s.GetString()! : "INITIAL") :
                (wd.TryGetProperty("process", out var p) ? p.GetString()! : "INITIAL");

            int h = wd.TryGetProperty("RemainHour", out var rh) ? rh.GetInt32() : 0;
            int m = wd.TryGetProperty("RemainMin", out var rm) ? rm.GetInt32() : 
                    (wd.TryGetProperty("remainTime", out var rt) ? rt.GetInt32() : 0);

            statusList.Add(new MachineDetailDto {
                DeviceId = deviceId,
                Alias = alias,
                CurState = state,
                TimeString = h > 0 ? $"{h}h {m}m" : $"{m}m",
                Online = snapshot.GetProperty("online").GetBoolean(),
                DeviceType = typeRaw == "211" ? "0" : "1"
            });
        }
        return statusList;
    }
}