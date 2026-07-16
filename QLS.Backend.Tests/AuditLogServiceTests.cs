using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using QLS.Backend.Data;
using QLS.Backend.Services;
using Xunit;

namespace QLS.Backend.Tests;

public class AuditLogServiceTests
{
    [Fact]
    public async Task LogAsync_ShouldPersistActorContextAndBoundLongHeaders()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers.UserAgent = new string('a', 600);
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, new string('u', 300)),
            new Claim(ClaimTypes.Role, "SystemAdmin")
        }, "Test"));

        var service = new AuditLogService(context, NullLogger<AuditLogService>.Instance);

        await service.LogAsync(
            httpContext,
            "payment_config.update",
            "PaymentConfig",
            Guid.NewGuid().ToString(),
            success: false,
            failureReason: new string('f', 700),
            metadata: new { HasSecretKey = true });

        var auditLog = await context.AuditLogs.SingleAsync();

        Assert.Equal("payment_config.update", auditLog.Action);
        Assert.Equal("PaymentConfig", auditLog.EntityType);
        Assert.False(auditLog.Success);
        Assert.Equal("SystemAdmin", auditLog.ActorRole);
        Assert.Equal("127.0.0.1", auditLog.IpAddress);
        Assert.Equal(256, auditLog.ActorUsername!.Length);
        Assert.Equal(512, auditLog.UserAgent!.Length);
        Assert.Equal(512, auditLog.FailureReason!.Length);
        Assert.Contains("HasSecretKey", auditLog.Metadata);
    }
}
