using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QLS.Backend.Services; // IMachineDetailService lives here

namespace QLS.Backend.Extensions;

public static class ServiceExtensions
{
    // 1. Hàm hiện tại của bạn: Tự động quét và Inject các Services
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssembliesOf(typeof(IMachineDetailService))
            .AddClasses(classes => classes.InNamespaces("QLS.Backend.Services"))
            .AsImplementedInterfaces()
            .WithScopedLifetime());
    }

    // 2. Thêm hàm cấu hình CORS ngay bên dưới
    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
    {
        // Đọc danh sách Domain được phép từ appsettings.json
        var allowedOrigins = configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowReactApp", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        return services;
    }
}