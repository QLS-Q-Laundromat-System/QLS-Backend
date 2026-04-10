using Microsoft.EntityFrameworkCore;
using QLS.Backend.Models;

namespace QLS.Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Machine> Machines { get; set; }
        public DbSet<StoreSetting> StoreSettings { get; set; }
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
                .HasOne<Store>()
                .WithMany(s => s.Machines)
                .HasForeignKey(m => m.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- Cấu hình quan hệ cho StoreSetting ---
            modelBuilder.Entity<StoreSetting>()
                .HasOne(s => s.Store)
                .WithOne(st => st.StoreSetting)
                .HasForeignKey<StoreSetting>(s => s.StoreId);

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
        }
    }
}
