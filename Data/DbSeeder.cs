using Microsoft.EntityFrameworkCore;
using QLS.Backend.Models;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // 1. Tự động chạy Migration
            await context.Database.MigrateAsync();

            // 2. Kiểm tra nếu chưa có Brand nào thì tạo bộ dữ liệu mẫu
            if (!await context.Brands.AnyAsync())
            {
                // -- TẠO BRAND MẪU --
                var defaultBrand = new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "QLS Premium Laundry",
                    Email = "contact@qlslaundry.com",
                    ContactPhone = "0901234567",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Brands.Add(defaultBrand);

                // -- TẠO STORE MẪU --
                var defaultStore = new Store
                {
                    Id = Guid.NewGuid(),
                    Name = "QLS Store - District 1",
                    Address = "123 Le Loi, District 1, HCMC",
                    BrandId = defaultBrand.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Stores.Add(defaultStore);

                // -- TẠO STORE SETTING --
                var defaultSetting = new StoreSetting
                {
                    StoreId = defaultStore.Id,
                    DryerStepMinutes = 10,
                    DryerStepPrice = 13000,
                    DryerMinInitialMinutes = 30,
                    DryerGracePeriodMinutes = 10
                };
                context.StoreSettings.Add(defaultSetting);

                // -- TẠO MÁY MẪU (MACHINES) --
                var machines = new List<Machine>
                {
                    new Machine { MachineId = "W001", BranchId = defaultStore.Id, Type = MachineType.Washer, Capacity = "10kg" },
                    new Machine { MachineId = "W002", BranchId = defaultStore.Id, Type = MachineType.Washer, Capacity = "10kg" },
                    new Machine { MachineId = "D001", BranchId = defaultStore.Id, Type = MachineType.Dryer, Capacity = "15kg" },
                    new Machine { MachineId = "D002", BranchId = defaultStore.Id, Type = MachineType.Dryer, Capacity = "15kg" }
                };
                context.Machines.AddRange(machines);

                // -- TẠO USER PROFILE MẪU --
                var testUser = new User
                {
                    Id = Guid.NewGuid(),
                    FullName = "Nguyễn Văn Khách",
                    Email = "khachhang@example.com",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Users.Add(testUser);

                // -- TẠO ACCOUNTS --
                // 1. SuperAdmin
                context.Accounts.Add(new Account
                {
                    Username = "admin@qls.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    Role = UserRole.SuperAdmin,
                    IsActive = true
                });

                // 2. Brand Admin + User Profile
                var brandAdminId = Guid.NewGuid();
                context.Users.Add(new User 
                { 
                    Id = brandAdminId, 
                    FullName = "Chủ chuỗi QLS Premium", 
                    Email = "owner@qls.com",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                context.Accounts.Add(new Account
                {
                    Id = brandAdminId,
                    Username = "owner@qls.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    Role = UserRole.AdminBranch,
                    BrandId = defaultBrand.Id,
                    IsActive = true
                });

                // 3. Customer Account (liên kết với testUser bằng ID)
                context.Accounts.Add(new Account
                {
                    Id = testUser.Id, // Dùng chung ID để map 1-1
                    Username = "0988888888",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    Role = UserRole.Customer,
                    IsActive = true
                });

                await context.SaveChangesAsync();
                Console.WriteLine("✅ Đã khởi tạo toàn bộ dữ liệu mẫu thành công!");
            }
        }
    }
}