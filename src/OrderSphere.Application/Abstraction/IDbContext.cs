using Microsoft.EntityFrameworkCore;
using OrderSphere.Domain.Entities;

namespace OrderSphere.Application.Abstraction;

public interface IDbContext
{
    public DbSet<Order> Orders { get; }
    public DbSet<OrderItem> OrderItems { get; }
    public DbSet<Cart> Carts { get; }
    public DbSet<CartItem> CartItems { get; }

    public Task BeginTransactionAsync(CancellationToken ct = default);
    public Task CommitAsync(CancellationToken ct = default);
    public Task RollbackAsync(CancellationToken ct = default);
}
