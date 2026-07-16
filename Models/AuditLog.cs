using System.ComponentModel.DataAnnotations;

namespace QLS.Backend.Models;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ActorUserId { get; set; }

    [MaxLength(256)]
    public string? ActorUsername { get; set; }

    [MaxLength(64)]
    public string? ActorRole { get; set; }

    [MaxLength(128)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(128)]
    public string EntityType { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? EntityId { get; set; }

    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [MaxLength(512)]
    public string? UserAgent { get; set; }

    public bool Success { get; set; }

    [MaxLength(512)]
    public string? FailureReason { get; set; }

    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
