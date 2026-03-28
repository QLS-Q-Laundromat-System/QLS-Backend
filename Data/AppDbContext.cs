using Microsoft.EntityFrameworkCore;

namespace QLS.Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Thêm các DbSet của bạn ở đây, ví dụ:
    // public DbSet<Washer> Washers { get; set; }
}
