using Microsoft.EntityFrameworkCore;
using QLS.Backend.Models;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Machine> Machines { get; set; }
        public DbSet<MachineSession> MachineSessions { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<StoreType> StoreTypes { get; set; }
        public DbSet<PriceList> PriceLists { get; set; }
        public DbSet<PriceListStoreType> PriceListStoreTypes { get; set; }
        public DbSet<PriceModePerKg> PriceModePerKgs { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<PriceModePerSession> PriceModePerSessions { get; set; }
        public DbSet<BrandLgCredential> BrandLgCredentials { get; set; }
        public DbSet<MachineSetting> MachineSettings { get; set; }
        public DbSet<DiscountCode> DiscountCodes { get; set; }
        public DbSet<DiscountCodeStore> DiscountCodeStores { get; set; }
        public DbSet<DiscountCodeUsage> DiscountCodeUsages { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<PaymentConfig> PaymentConfigs { get; set; }
        public DbSet<LoyaltyCustomer> LoyaltyCustomers { get; set; }
        public DbSet<PointClaimToken> PointClaimTokens { get; set; }
        public DbSet<LoyaltyPointTransaction> LoyaltyPointTransactions { get; set; }
        public DbSet<MachineNotification> MachineNotifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Cấu hình quan hệ cho Brand ---
            modelBuilder.Entity<Store>()
                .HasOne(s => s.Brand)
                .WithMany(b => b.Stores)
                .HasForeignKey(s => s.BrandId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StoreType>()
                .HasOne(st => st.Brand)
                .WithMany(b => b.StoreTypes)
                .HasForeignKey(st => st.BrandId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- Cấu hình quan hệ cho Store và StoreType ---
            modelBuilder.Entity<Store>()
                .HasOne(s => s.StoreType)
                .WithMany(st => st.Stores)
                .HasForeignKey(s => s.StoreTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Cấu hình quan hệ cho Account ---
            modelBuilder.Entity<Account>()
                .HasOne(a => a.Brand)
                .WithMany(b => b.Accounts)
                .HasForeignKey(a => a.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Account>()
                .HasOne(a => a.Store)
                .WithMany()
                .HasForeignKey(a => a.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Cấu hình quan hệ cho Machine ---
            modelBuilder.Entity<Machine>()
                .HasOne(m => m.Store)
                .WithMany(s => s.Machines)
                .HasForeignKey(m => m.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- Cấu hình quan hệ cho MachineSession ---
            modelBuilder.Entity<MachineSession>()
                .HasOne(ms => ms.User)
                .WithMany(u => u.MachineSessions)
                .HasForeignKey(ms => ms.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MachineSession>()
                .HasOne(ms => ms.Machine)
                .WithMany()
                .HasForeignKey(ms => ms.MachineId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MachineSession>()
                .HasOne(ms => ms.Store)
                .WithMany()
                .HasForeignKey(ms => ms.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MachineSession>()
                .Property(ms => ms.PricePaid)
                .HasPrecision(18, 2);

            modelBuilder.Entity<MachineNotification>()
                .HasOne(n => n.Store)
                .WithMany()
                .HasForeignKey(n => n.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MachineNotification>()
                .HasOne(n => n.Machine)
                .WithMany()
                .HasForeignKey(n => n.MachineId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MachineNotification>()
                .HasOne(n => n.Session)
                .WithMany()
                .HasForeignKey(n => n.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MachineNotification>()
                .HasIndex(n => new { n.SessionId, n.Type })
                .IsUnique();

            modelBuilder.Entity<MachineNotification>()
                .HasIndex(n => new { n.StoreId, n.IsRead, n.CreatedAt });

            // --- Cấu hình quan hệ 1-1 giữa Brand và BrandLgCredential ---
            modelBuilder.Entity<BrandLgCredential>()
                .HasOne(c => c.Brand)
                .WithOne(b => b.LgCredential)
                .HasForeignKey<BrandLgCredential>(c => c.BrandId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- Cấu hình quan hệ cho PaymentConfig ---
            modelBuilder.Entity<PaymentConfig>()
                .HasOne(p => p.Brand)
                .WithMany(b => b.PaymentConfigs)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- Cấu hình quan hệ cho LoyaltyCustomer ---
            modelBuilder.Entity<LoyaltyCustomer>()
                .HasOne(c => c.Brand)
                .WithMany()
                .HasForeignKey(c => c.BrandId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LoyaltyCustomer>()
                .HasIndex(c => new { c.BrandId, c.ZaloUserId })
                .IsUnique();

            // --- Cấu hình quan hệ cho PointClaimToken ---
            modelBuilder.Entity<PointClaimToken>()
                .HasOne(t => t.Brand)
                .WithMany()
                .HasForeignKey(t => t.BrandId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PointClaimToken>()
                .HasOne(t => t.MachineSession)
                .WithMany()
                .HasForeignKey(t => t.MachineSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PointClaimToken>()
                .HasOne(t => t.PaymentTransaction)
                .WithMany()
                .HasForeignKey(t => t.PaymentTransactionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PointClaimToken>()
                .HasOne(t => t.ClaimedByCustomer)
                .WithMany()
                .HasForeignKey(t => t.ClaimedByCustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PointClaimToken>()
                .HasIndex(t => t.Token)
                .IsUnique();

            modelBuilder.Entity<PointClaimToken>()
                .HasIndex(t => t.MachineSessionId)
                .IsUnique();

            // --- Cấu hình quan hệ cho LoyaltyPointTransaction ---
            modelBuilder.Entity<LoyaltyPointTransaction>()
                .HasOne(t => t.Brand)
                .WithMany()
                .HasForeignKey(t => t.BrandId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LoyaltyPointTransaction>()
                .HasOne(t => t.Customer)
                .WithMany(c => c.PointTransactions)
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LoyaltyPointTransaction>()
                .HasOne(t => t.MachineSession)
                .WithMany()
                .HasForeignKey(t => t.MachineSessionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Earn cho mỗi session chỉ được phép 1 lần.
            modelBuilder.Entity<LoyaltyPointTransaction>()
                .HasIndex(t => new { t.MachineSessionId, t.Type })
                .IsUnique()
                .HasFilter("\"MachineSessionId\" IS NOT NULL AND \"Type\" = 0");

            // --- Cấu hình Inheritance cho PriceModePerSession (TPH) ---
            modelBuilder.Entity<PriceModePerSession>()
                .HasDiscriminator<MachineType>("MachineType")
                .HasValue<WasherPriceMode>(MachineType.Washer)
                .HasValue<DryerPriceMode>(MachineType.Dryer);

            // --- Cấu hình quan hệ cho MachineSetting ---
            modelBuilder.Entity<MachineSetting>()
                .HasOne(ms => ms.Machine)
                .WithOne(m => m.Setting)
                .HasForeignKey<MachineSetting>(ms => ms.MachineId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- Cấu hình quan hệ cho DiscountCode ---
            modelBuilder.Entity<DiscountCodeStore>()
                .HasKey(dcs => new { dcs.DiscountCodeId, dcs.StoreId });

            modelBuilder.Entity<DiscountCodeStore>()
                .HasOne(dcs => dcs.DiscountCode)
                .WithMany(dc => dc.DiscountCodeStores)
                .HasForeignKey(dcs => dcs.DiscountCodeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DiscountCodeStore>()
                .HasOne(dcs => dcs.Store)
                .WithMany()
                .HasForeignKey(dcs => dcs.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DiscountCodeUsage>()
                .HasOne(dcu => dcu.DiscountCode)
                .WithMany(dc => dc.DiscountCodeUsages)
                .HasForeignKey(dcu => dcu.DiscountCodeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
