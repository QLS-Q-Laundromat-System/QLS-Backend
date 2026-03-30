namespace QLS.Backend.DTOs;

public class WasherStatusDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string CurState { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string CourseNum { get; set; } = string.Empty;
    public int RemainHour { get; set; }
    public int RemainMin { get; set; }
    public int RemainTime { get; set; }
    public string TimeString { get; set; } = "--";
    public bool Online { get; set; }
    public int DeviceType { get; set; }
}
