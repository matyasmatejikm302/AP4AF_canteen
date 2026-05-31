using Microsoft.EntityFrameworkCore;
using UTB.Minute.Db.Entities;

namespace UTB.Minute.Db;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Nastavení přesnosti pro cenu jídla
        modelBuilder.Entity<Meal>()
            .Property(m => m.Price)
            .HasPrecision(18, 2);
    }
}