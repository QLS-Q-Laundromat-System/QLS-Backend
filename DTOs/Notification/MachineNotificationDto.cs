namespace QLS.Backend.DTOs.Notification;

public class MachineNotificationDto
{
    public Guid Id { get; set; }
    public Guid MachineId { get; set; }
    public Guid SessionId { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string MachineType { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}
