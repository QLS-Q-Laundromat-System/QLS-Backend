namespace QLS.Backend.DTOs;

public class WasherStatusDto
{
    public string CurState { get; set; } = string.Empty;
    public string CourseNum { get; set; } = string.Empty;
    public int RemainHour { get; set; }
    public int RemainMin { get; set; }
    public string TimeString { get; set; } = "--";
}
