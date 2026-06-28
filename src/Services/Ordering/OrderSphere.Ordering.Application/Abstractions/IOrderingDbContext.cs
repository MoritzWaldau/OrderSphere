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
    DbSet<ReturnRequest> ReturnRequests { get; }

    /// <summary>
    /// Stages an integration event in the outbox table; dispatched to Service Bus by the
    /// OutboxDispatcher after the surrounding SaveChanges commits.
    /// </summary>
    void AddOutboxMessage(string type, string content);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
