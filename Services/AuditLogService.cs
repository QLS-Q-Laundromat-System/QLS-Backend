using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using QLS.Backend.Data;
using QLS.Backend.Models;

namespace QLS.Backend.Services;

public interface IAuditLogService
{
    Task LogAsync(
        HttpContext httpContext,
        string action,
        string entityType,
        string? entityId,
        bool success,
        string? failureReason = null,
        object? metadata = null,
        CancellationToken cancellationToken = default);
}

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(AppDbContext context, ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(
        HttpContext httpContext,
        string action,
        string entityType,
        string? entityId,
        bool success,
        string? failureReason = null,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = httpContext.User;
            var actorUserId = TryGetGuidClaim(user, ClaimTypes.NameIdentifier) ?? TryGetGuidClaim(user, "id");
            var userAgent = Truncate(httpContext.Request.Headers.UserAgent.ToString(), 512);

            _context.AuditLogs.Add(new AuditLog
            {
                ActorUserId = actorUserId,
                ActorUsername = Truncate(user?.FindFirst(ClaimTypes.Name)?.Value, 256),
                ActorRole = Truncate(user?.FindFirst(ClaimTypes.Role)?.Value, 64),
                Action = Truncate(action, 128) ?? string.Empty,
                EntityType = Truncate(entityType, 128) ?? string.Empty,
                EntityId = Truncate(entityId, 128),
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
                Success = success,
                FailureReason = Truncate(failureReason, 512),
                Metadata = metadata == null ? null : JsonSerializer.Serialize(metadata),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for action {Action}.", action);
        }
    }

    private static Guid? TryGetGuidClaim(ClaimsPrincipal? user, string claimType)
    {
        var value = user?.FindFirst(claimType)?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        return value == null || value.Length <= maxLength ? value : value[..maxLength];
    }
}
