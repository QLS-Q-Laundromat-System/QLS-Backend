using Microsoft.EntityFrameworkCore;
using QLS.Backend.Models;

namespace QLS.Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Machine> Machines { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<StoreSetting> StoreSettings { get; set; }
        public DbSet<MachineSession> MachineSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Cấu hình thêm các quan hệ bảng (Fluent API) ở đây nếu cần
        }
    }
}