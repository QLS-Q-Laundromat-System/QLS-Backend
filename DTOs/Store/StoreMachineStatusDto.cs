namespace QLS.Backend.DTOs.Store;

public class StoreMachineStatusDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Capacity { get; set; } = string.Empty;
    public string? LgDeviceId { get; set; }

    public bool Online { get; set; }
    public string StatusText { get; set; } = string.Empty;

    public string? CurState { get; set; }
    public string? Course { get; set; }
    public string? CourseNum { get; set; }
    public int? RemainHour { get; set; }
    public int? RemainMin { get; set; }
    public int? RemainTime { get; set; }
    public string? Process { get; set; }
}