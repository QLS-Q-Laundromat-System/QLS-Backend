using Microsoft.EntityFrameworkCore;
using QLS.Backend.Models;

namespace QLS.Backend.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // Tự động chạy Migration để tạo bảng nếu chưa có
            await context.Database.MigrateAsync();

            // Kiểm tra xem trong DB đã có tài khoản SuperAdmin nào chưa
            bool hasAdmin = await context.UserAdmins.AnyAsync(u => u.Role == "SuperAdmin");

            if (!hasAdmin)
            {
                // Nếu chưa có, tạo mới một tài khoản mặc định
                var superAdmin = new UserAdmin
                {
                    FullName = "Admin Tổng Hệ Thống",
                    Email = "admin@qls.com",
                    PasswordHash = "123456", // Tạm thời để số cho dễ test, sau này sẽ mã hóa sau
                    Role = "SuperAdmin",
                    OwnerId = null, // SuperAdmin không thuộc về cửa hàng nào cả
                    BranchId = null
                };

                context.UserAdmins.Add(superAdmin);
                await context.SaveChangesAsync();
            }
        }
    }
}