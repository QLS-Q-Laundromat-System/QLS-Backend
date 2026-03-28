using Microsoft.EntityFrameworkCore;
using QLS.Backend.Models;

namespace QLS.Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Thêm các DbSet của bạn ở đây:
    public DbSet<Store> Stores { get; set; }
    public DbSet<Machine> Machines { get; set; }
}
