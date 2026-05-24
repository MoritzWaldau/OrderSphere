using Microsoft.EntityFrameworkCore;
using OrderSphere.Basket.Domain.Entities;

namespace OrderSphere.Basket.Infrastructure.Persistence;

public sealed class BasketDbContext(DbContextOptions<BasketDbContext> options) : DbContext(options)
{
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BasketDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
