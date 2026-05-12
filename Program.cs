using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.Extensions;
using QLS.Backend.Services;
using QLS.Backend.Services.LgService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddConfigSwagger();

// Khai báo kết nối Database (Sử dụng PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddApplicationServices();
builder.Services.AddCustomCors(builder.Configuration);

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
    });

builder.Services.AddAuthorization();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpClient<LgApiClient>();

var app = builder.Build();


// =====================================================================
// TỰ ĐỘNG CHẠY MIGRATION & SEED DỮ LIỆU KHI KHỞI ĐỘNG
// =====================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        // Apply tất cả pending migrations vào database
        await context.Database.MigrateAsync();
        logger.LogInformation("✅ Migration hoàn tất!");

        await QLS.Backend.Data.DbSeeder.SeedAsync(context);
        logger.LogInformation("✅ Seed dữ liệu hoàn tất!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Có lỗi xảy ra khi migration hoặc seed dữ liệu.");
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
app.UseMiddleware<QLS.Backend.Middlewares.GlobalExceptionMiddleware>();

// Swagger luôn bật (Development & Production)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "QLS Backend API v1");
    c.RoutePrefix = string.Empty;
});

// Health Check endpoint cho CI/CD pipeline
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Kích hoạt Middleware CORS
app.UseCors("AllowAll");

// app.UseHttpsRedirection(); // Đã tắt do chỉ test HTTP nội bộ

app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();