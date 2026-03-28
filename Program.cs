using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data;
using QLS.Backend.Interfaces;
using QLS.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Đăng ký cấu hình Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Đăng ký CSDL PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký HttpClient cho WasherService
builder.Services.AddHttpClient<IWasherService, WasherService>();

// Thêm CORS cho phép Frontend gọi thoải mái
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ----- KIỂM TRA KẾT NỐI DATABASE VÀO LÚC KHỞI ĐỘNG -----
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        Console.WriteLine("Đang kiểm tra kết nối với PostgreSQL...");
        bool canConnect = dbContext.Database.CanConnect();
        if (canConnect)
        {
            Console.WriteLine("✅ THÀNH CÔNG: Đã kết nối được Database PostgreSQL!");
        }
        else
        {
            Console.WriteLine("❌ THẤT BẠI: Không thể kết nối Database PostgreSQL. Vui lòng kiểm tra lại Host, Port, Username, Password.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ LỖI KẾT NỐI: {ex.Message}");
    }
}
// --------------------------------------------------------

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "QLS Backend API v1");
        c.RoutePrefix = string.Empty; // Hiển thị Swagger ngay tại trang chủ localhost:5078/
    });
}

// Kích hoạt Middleware CORS
app.UseCors("AllowAll");

// app.UseHttpsRedirection(); // Đã tắt do chỉ test HTTP nội bộ

app.UseAuthorization();

app.MapControllers();

app.Run();
