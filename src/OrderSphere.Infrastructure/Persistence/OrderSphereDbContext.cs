using Microsoft.EntityFrameworkCore;
using OrderSphere.Domain.Entities;

namespace OrderSphere.Infrastructure.Persistence;

public class OrderSphereDbContext(DbContextOptions<OrderSphereDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderSphereDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}