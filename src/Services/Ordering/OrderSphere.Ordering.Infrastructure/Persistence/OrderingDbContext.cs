using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OrderSphere.BuildingBlocks.EventBus.Inbox;
using OrderSphere.Ordering.Domain.Entities;
using OrderSphere.Ordering.Infrastructure.Outbox;

namespace OrderSphere.Ordering.Infrastructure.Persistence;

public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options)
    : DbContext(options), IOrderingDbContext
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public void AddOutboxMessage(string type, string content)
        => OutboxMessages.Add(new OutboxMessage { Type = type, Content = content });
    internal DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    private IDbContextTransaction? _transaction;

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
            throw new InvalidOperationException("A transaction is already active.");

        _transaction = await Database.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("No active transaction.");

        try
        {
            await SaveChangesAsync(ct);
            await _transaction.CommitAsync(ct);
        }
        catch
        {
            await _transaction.DisposeAsync();
            _transaction = null;
            throw;
        }

        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction == null)
            return;

        try
        {
            await _transaction.RollbackAsync(ct);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
