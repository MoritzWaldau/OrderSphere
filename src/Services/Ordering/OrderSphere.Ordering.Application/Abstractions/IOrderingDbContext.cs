using Microsoft.EntityFrameworkCore;
using OrderSphere.Ordering.Domain.Entities;
using OrderSphere.Ordering.Domain.ReadModels;

namespace OrderSphere.Ordering.Application.Abstractions;

public interface IOrderingDbContext
{
    DbSet<OrderView> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Coupon> Coupons { get; }
    DbSet<OrderSaga> OrderSagas { get; }
    DbSet<OrderHistoryEntry> OrderHistory { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
