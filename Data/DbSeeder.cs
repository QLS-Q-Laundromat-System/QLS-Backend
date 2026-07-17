using Microsoft.EntityFrameworkCore;
using QLS.Backend.Models;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // 1. Tự động chạy Migration để cập nhật cấu trúc DB mới nhất (không xóa dữ liệu cũ)
            await context.Database.MigrateAsync();

            Console.WriteLine("====== [SYSTEM] ĐÃ CẬP NHẬT MIGRATIONS (NẾU CÓ) ======");

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
                    Address = "123 Le Loi, District 1, HCMC",
                    Logo = "https://via.placeholder.com/150", 
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Brands.Add(defaultBrand);

                // -- TẠO STORE TYPE --
                var premiumType = new StoreType
                {
                    Id = Guid.NewGuid(),
                    Name = "Premium District 1",
                    Level = 2,
                    BrandId = defaultBrand.Id
                };
                var standardType = new StoreType
                {
                    Id = Guid.NewGuid(),
                    Name = "Standard QLS",
                    Level = 1,
                    BrandId = defaultBrand.Id
                };
                context.StoreTypes.AddRange(premiumType, standardType);

                // -- TẠO CÁC STORE THẬT TỪ LG --
                var qLaundromatStore = new Store
                {
                    Id = Guid.NewGuid(),
                    Name = "Q Laundromat",
                    Address = "Vinhomes Grand Park, Nguyen Xien, s202, Thu duc, Ho Chi Minh",
                    Phone = "035-926-1605",
                    Email = "nguyenquocan1010@gmail.com",
                    StoreId = "c7863f0d53ac48bfb04d4b1367e664b7",
                    BrandId = defaultBrand.Id,
                    StoreTypeId = premiumType.Id, // Gán hạng Premium
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                var qls1Store = new Store
                {
                    Id = Guid.NewGuid(),
                    Name = "QLS-1",
                    Address = "vinhome1, Quan 9, Ho Chi Minh",
                    Phone = "0862789211",
                    Email = "store1@gmail.com",
                    StoreId = "d989472b67ad421ba2e6dbbf43dfcebd",
                    BrandId = defaultBrand.Id,
                    StoreTypeId = premiumType.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Stores.AddRange(qLaundromatStore, qls1Store);

                // -- TẠO MÁY LG ĐÃ BIẾT --
                context.Machines.Add(new Machine
                {
                    Id = Guid.NewGuid(),
                    StoreId = qLaundromatStore.Id,
                    LgDeviceId = "fe94d7ca-e9c7-1d0c-a818-f414bff7003b",
                    Name = "Dryer_new",
                    Type = MachineType.Dryer,
                    Capacity = "LG_COMMERCIAL"
                });

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
                // 1. SystemAdmin
                context.Accounts.Add(new Account
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    FullName = "Hệ thống QLS",
                    Email = "admin@qls.com",
                    Role = UserRole.SystemAdmin,
                    IsActive = true
                });

                // 2. Brand Admin
                var brandAdminId = Guid.NewGuid();
                context.Accounts.Add(new Account
                {
                    Id = brandAdminId,
                    Username = "owner@qls.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    FullName = "Chủ chuỗi QLS Premium",
                    Email = "owner@qls.com",
                    Role = UserRole.BrandAdmin,
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

                // -- TẠO TIMESLOT (Giờ vàng) --
                var happyHourSlot = new TimeSlot
                {
                    Id = Guid.NewGuid(),
                    BrandId = defaultBrand.Id,
                    Name = "Happy Hour Sáng (8h-11h)",
                    Description = "Giảm giá đặc biệt khung giờ sáng",
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(11, 0),
                    DayMask = DayOfWeekMask.Weekdays | DayOfWeekMask.Saturday
                };
                context.TimeSlots.Add(happyHourSlot);

                // -- TẠO BẢNG GIÁ (PriceList) --
                var mainPriceList = new PriceList
                {
                    Id = Guid.NewGuid(),
                    BrandId = defaultBrand.Id,
                    Code = "STD_2026",
                    Name = "Bảng giá Niêm yết 2026",
                    PromotionLabel = "Giá tốt mỗi ngày",
                    ValidFrom = new DateOnly(2026, 1, 1),
                    Status = PriceListStatus.Active,
                    Priority = 10,
                    Currency = Currency.VND,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                context.PriceLists.Add(mainPriceList);

                // Gán Bảng giá cho hạng cửa hàng Premium
                context.PriceListStoreTypes.Add(new PriceListStoreType
                {
                    PriceListId = mainPriceList.Id,
                    StoreTypeId = premiumType.Id,
                    OverridePriority = 20 // Ưu tiên cao hơn bảng giá gốc
                });

                // -- CẤU HÌNH GIÁ MODE 1 (Per Kg) --
                context.PriceModePerKgs.AddRange(new List<PriceModePerKg>
                {
                    new PriceModePerKg { PriceListId = mainPriceList.Id, MachineType = MachineType.Washer, MinKg = 0, MaxKg = 5, UnitPrice = 15000, PricePer = PricePerType.PerKg, SortOrder = 1 },
                    new PriceModePerKg { PriceListId = mainPriceList.Id, MachineType = MachineType.Washer, MinKg = 5, MaxKg = 10, UnitPrice = 13000, PricePer = PricePerType.PerKg, SortOrder = 2 },
                    new PriceModePerKg { PriceListId = mainPriceList.Id, MachineType = MachineType.Washer, MinKg = 10, MaxKg = null, UnitPrice = 120000, PricePer = PricePerType.Flat, SortOrder = 3 }
                });

                // -- CẤU HÌNH GIÁ MODE 2 (Per Session) --
                context.PriceModePerSessions.AddRange(new List<PriceModePerSession>
                {
                    // Giá Happy Hour cho máy 10kg
                    new WasherPriceMode { PriceListId = mainPriceList.Id, MachineCapacityKg = 10, Price = 30000, DurationMinutes = 30, TimeSlotId = happyHourSlot.Id },
                    // Giá mặc định cho máy 10kg
                    new WasherPriceMode { PriceListId = mainPriceList.Id, MachineCapacityKg = 10, Price = 50000, DurationMinutes = 30, TimeSlotId = null },
                    // Giá máy sấy
                    new DryerPriceMode { PriceListId = mainPriceList.Id, MachineCapacityKg = 15, Price = 40000, DurationMinutes = 45, TimeSlotId = null }
                });

                await context.SaveChangesAsync();
                Console.WriteLine("✅ Đã khởi tạo toàn bộ dữ liệu mẫu thành công!");
            }
        }
    }
}
