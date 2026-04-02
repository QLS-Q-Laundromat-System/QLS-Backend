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
        public DbSet<BranchSetting> BranchSettings { get; set; }
        public DbSet<MachineSession> MachineSessions { get; set; }
        public DbSet<UserAdmin> UserAdmins { get; set; }
        public DbSet<Owner> Owners { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình thêm các quan hệ bảng (Fluent API) ở đây nếu cần
            modelBuilder.Entity<UserAdmin>()
                .HasOne(u => u.Owner)
                .WithMany(o => o.UserAdmins)
                .HasForeignKey(u => u.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserAdmin>()
                .HasOne(u => u.Branch)
                .WithMany(b => b.UserAdmins)
                .HasForeignKey(u => u.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Branch>()
                .HasOne(b => b.Owner)
                .WithMany(o => o.Branches)
                .HasForeignKey(b => b.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BranchSetting>()
                .HasOne(s => s.Branch)
                .WithOne(b => b.BranchSetting)
                .HasForeignKey<BranchSetting>(s => s.BranchId);

            modelBuilder.Entity<MachineSession>()
                .HasOne(ms => ms.User)
                .WithMany(u => u.MachineSessions)
                .HasForeignKey(ms => ms.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
