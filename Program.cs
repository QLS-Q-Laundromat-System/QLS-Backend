using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.Extensions;
using QLS.Backend.Services;
using QLS.Backend.Services.LgService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Threading.RateLimiting;
using System.Text;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

if (!builder.Environment.IsDevelopment())
{
    var requiredConfiguration = new[]
    {
        "ConnectionStrings:DefaultConnection",
        "Jwt:Key",
        "LgApi:ApiKey",
        "Zalo:AppSecretKey",
        "ReverseProxy:KnownProxies:0"
    };

    var missingConfiguration = requiredConfiguration
        .Where(key => string.IsNullOrWhiteSpace(builder.Configuration[key]))
        .ToArray();

    if (missingConfiguration.Length > 0)
    {
        throw new InvalidOperationException(
            $"Missing required production configuration: {string.Join(", ", missingConfiguration)}");
    }
}
// Add services to the container.
builder.Services.AddControllers();
var authRateLimit = builder.Configuration.GetValue("RateLimiting:Auth:PermitLimit", 10);
var authRateWindowMinutes = builder.Configuration.GetValue("RateLimiting:Auth:WindowMinutes", 1);
var webhookRateLimit = builder.Configuration.GetValue("RateLimiting:Webhook:PermitLimit", 120);
var webhookRateWindowMinutes = builder.Configuration.GetValue("RateLimiting:Webhook:WindowMinutes", 1);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientPartition(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authRateLimit,
                Window = TimeSpan.FromMinutes(authRateWindowMinutes),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("webhook", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientPartition(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = webhookRateLimit,
                Window = TimeSpan.FromMinutes(webhookRateWindowMinutes),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});
builder.Services.AddConfigSwagger(builder.Environment);
builder.Services.AddHostedService<QLS.Backend.Services.Machine.MachineStatusMonitoringService>();
builder.Services.AddHostedService<QLS.Backend.Services.Ziggbee.MqttListenerService>();
builder.Services.AddHostedService<QLS.Backend.Services.Loyalty.LoyaltyPointExpiryService>();
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();

    var knownProxies = builder.Configuration
        .GetSection("ReverseProxy:KnownProxies")
        .Get<string[]>() ?? Array.Empty<string>();

    foreach (var knownProxy in knownProxies)
    {
        if (IPAddress.TryParse(knownProxy, out var address))
        {
            options.KnownProxies.Add(address);
        }
    }
});

// Khai báo kết nối Database (Sử dụng PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddApplicationServices();
builder.Services.AddCustomCors(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<QLS.Backend.Services.Machine.IHardwareTrackerService, QLS.Backend.Services.Machine.HardwareTrackerService>();

// Cấu hình JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true, // Quan trọng: Tự động từ chối token hết hạn
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                if (string.IsNullOrWhiteSpace(authHeader))
                {
                    return Task.CompletedTask;
                }

                const string bearerPrefix = "Bearer ";
                context.Token = authHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
                    ? authHeader[bearerPrefix.Length..].Trim()
                    : authHeader.Trim();

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<LgApiClient>();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync();
        await QLS.Backend.Data.DbSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Development database migration or seed failed.");
        throw;
    }
}

// ... existing database connection check code ...

// =====================================================================
// ĐOẠN CODE KIỂM TRA KẾT NỐI DATABASE NGAY KHI KHỞI ĐỘNG SERVER
// =====================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Lấy AppDbContext ra từ môi trường
        var context = services.GetRequiredService<AppDbContext>();

        // Thực hiện thử kết nối
        if (context.Database.CanConnect())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("=========================================");
            Console.WriteLine("✅ KẾT NỐI DATABASE THÀNH CÔNG!");
            Console.WriteLine("=========================================");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ KHÔNG THỂ KẾT NỐI DATABASE. Vui lòng kiểm tra lại Connection String.");
            Console.ResetColor();
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ LỖI TRONG QUÁ TRÌNH KẾT NỐI DATABASE: {ex.Message}");
        Console.ResetColor();
    }
}
// =====================================================================

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();
app.UseMiddleware<QLS.Backend.Middlewares.GlobalExceptionMiddleware>();
app.UseRouting();
app.UseCors("AllowReactApp");
app.UseRateLimiter();

// Swagger luôn bật (Development & Production)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "QLS Backend dev v1");
        c.RoutePrefix = "swagger";
    });
}

// Redirect root to Swagger to avoid 404 when probing "/"
app.MapGet("/", () => app.Environment.IsDevelopment()
        ? Results.Redirect("/swagger")
        : Results.Ok(new { status = "healthy" }))
    .AllowAnonymous();

// Health Check endpoint cho CI/CD pipeline
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .AllowAnonymous();

// Endpoint kiểm tra trạng thái migration
if (app.Environment.IsDevelopment())
{
    app.MapGet("/db-status", async (AppDbContext db) =>
    {
        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        return Results.Ok(new
        {
            appliedCount = applied.Count,
            pendingCount = pending.Count,
            lastApplied = applied.LastOrDefault(),
            pendingMigrations = pending
        });
    });
}

// app.UseHttpsRedirection(); // Đã tắt do chỉ test HTTP nội bộ

app.UseAuthentication();
app.UseAuthorization();

// Centralized structured request logging (captures Method, Path, StatusCode, Latency, and JWT UserId)
app.UseMiddleware<QLS.Backend.Middlewares.RequestLoggingMiddleware>();

app.MapControllers();

app.Run();

static string GetClientPartition(HttpContext httpContext)
{
    return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-client";
}
