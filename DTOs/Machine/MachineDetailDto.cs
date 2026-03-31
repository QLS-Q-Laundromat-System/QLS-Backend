using System.Text.Json.Serialization;

namespace QLS.Backend.DTOs;

public class MachineDetailDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CurState { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Course { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CourseNum { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RemainHour { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RemainMin { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RemainTime { get; set; }
    
    public bool Online { get; set; }
    public string DeviceType { get; set; } = string.Empty;
}