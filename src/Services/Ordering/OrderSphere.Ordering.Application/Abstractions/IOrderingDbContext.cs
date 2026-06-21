using Microsoft.EntityFrameworkCore;
using OrderSphere.Ordering.Domain.Entities;

namespace OrderSphere.Ordering.Application.Abstractions;

public interface IOrderingDbContext
{
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Coupon> Coupons { get; }
    DbSet<OrderSaga> OrderSagas { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
