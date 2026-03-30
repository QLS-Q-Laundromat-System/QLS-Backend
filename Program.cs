using Microsoft.EntityFrameworkCore;
using QLS.Backend.Data; 
using QLS.Backend.Extensions;
using QLS.Backend.Services;
using QLS.Backend.Integrations.LG;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Khai báo kết nối Database (Sử dụng PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddApplicationServices();    

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<LgApiClient>();

var app = builder.Build();

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
if (app.Environment.IsDevelopment())
{
    // Enable middleware to serve generated Swagger as a JSON endpoint.
    app.UseSwagger();
    
    // Enable middleware to serve swagger-ui
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "QLS Backend API v1");
        c.RoutePrefix = string.Empty; // Hiển thị Swagger trực tiếp tại http://localhost:5078/
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();