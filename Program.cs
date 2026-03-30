using Microsoft.EntityFrameworkCore;
// using Thư_mục_chứa_AppDbContext_của_bạn; 

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Khai báo kết nối Database (Ví dụ dùng SQL Server hoặc PostgreSQL)
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// =====================================================================
// ĐOẠN CODE KIỂM TRA KẾT NỐI DATABASE NGAY KHI KHỞI ĐỘNG SERVER
// =====================================================================
// using (var scope = app.Services.CreateScope())
// {
//     var services = scope.ServiceProvider;
//     try
//     {
//         // Lấy AppDbContext ra từ môi trường
//         var context = services.GetRequiredService<AppDbContext>();
//         
//         // Thực hiện thử kết nối
//         if (context.Database.CanConnect())
//         {
//             Console.ForegroundColor = ConsoleColor.Green;
//             Console.WriteLine("=========================================");
//             Console.WriteLine("✅ KẾT NỐI DATABASE THÀNH CÔNG!");
//             Console.WriteLine("=========================================");
//             Console.ResetColor();
//         }
//         else
//         {
//             Console.ForegroundColor = ConsoleColor.Red;
//             Console.WriteLine("❌ KHÔNG THỂ KẾT NỐI DATABASE. Vui lòng kiểm tra lại Connection String.");
//             Console.ResetColor();
//         }
//     }
//     catch (Exception ex)
//     {
//         Console.ForegroundColor = ConsoleColor.Red;
//         Console.WriteLine($"❌ LỖI TRONG QUÁ TRÌNH KẾT NỐI DATABASE: {ex.Message}");
//         Console.ResetColor();
//     }
// }
// =====================================================================

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();