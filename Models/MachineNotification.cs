using System.ComponentModel.DataAnnotations;

namespace QLS.Backend.Models;

public class MachineNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid StoreId { get; set; }

    public Store? Store { get; set; }

    [Required]
    public Guid MachineId { get; set; }

    public Machine? Machine { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    public MachineSession? Session { get; set; }

    [Required, MaxLength(32)]
    public string Type { get; set; } = string.Empty;

    [Required, MaxLength(240)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; }
}
