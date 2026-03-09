using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OrderSphere.Domain.Entities;

namespace OrderSphere.Application.Abstraction;

public interface IDbContext
{
    public DbSet<Product> Products { get; }
    public DbSet<Order> Orders { get; }
    public DbSet<OrderItem> OrderItems { get; }

    public bool IsTransactionRunning();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
    public Task CommitAsync(IDbContextTransaction transaction, CancellationToken ct = default);
    public Task RollbackAsync(CancellationToken ct = default);

}
