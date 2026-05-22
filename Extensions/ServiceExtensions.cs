using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using QLS.Backend.Services;
using QLS.Backend.Services.LgServices.authToken;
using QLS.Backend.Services.LgService;
// IMachineDetailService lives here

namespace QLS.Backend.Extensions;

public static class ServiceExtensions
{
    // 1. Hàm hiện tại của bạn: Tự động quét và Inject các Services
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssembliesOf(typeof(IMachineDetailService))
            .AddClasses(classes => classes
                .InNamespaces(
                    "QLS.Backend.Services",
                    "QLS.Backend.Services.LgServices.authToken",
                    "QLS.Backend.Services.Brand",
                    "QLS.Backend.Services.MachineSettings",
                    "QLS.Backend.Services.Machine",
                    "QLS.Backend.Services.LgService",
                    "QLS.Backend.Services.Revenue",
                    "QLS.Backend.Services.Pricing",
                    "QLS.Backend.Services.Dashboard",
                    "QLS.Backend.Services.Stores",
                    "QLS.Backend.Services.DiscountCode",
                    "QLS.Backend.Services.Loyalty",
                    "QLS.Backend.Services.Payment",
                    "QLS.Backend.Services.Zalo"
                )
                .Where(type => !typeof(Microsoft.Extensions.Hosting.IHostedService).IsAssignableFrom(type))) // Loại trừ các BackgroundService
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Đăng ký HttpClient cho LgAuthTokenService và LgApiClient
        services.AddHttpClient<LgAuthTokenService>();
        services.AddHttpClient<LgApiClient>();
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
                      .SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost" || allowedOrigins.Contains(origin))
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddConfigSwagger(this IServiceCollection services, IWebHostEnvironment environment)
    {
        var swaggerTitle = environment.IsDevelopment() ? "QLS Backend dev" : "QLS Backend";

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = swaggerTitle, Version = "v1" });
            options.CustomSchemaIds(type => type.FullName ?? type.Name);

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Dán JWT token vào đây. Swagger sẽ tự thêm tiền tố Bearer.",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", document),
                    new List<string>()
                }
            });
        });

        return services;
    }
}
