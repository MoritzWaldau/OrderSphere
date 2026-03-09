using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OrderSphere.Application.Abstraction;
using OrderSphere.Domain.Entities;
using OrderSphere.Domain.Outbox;

namespace OrderSphere.Infrastructure.Persistence;

public class OrderSphereDbContext(DbContextOptions<OrderSphereDbContext> options) : DbContext(options), IDbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        return await Database.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(IDbContextTransaction transaction, CancellationToken ct = default)
    {
        await transaction.CommitAsync(ct);
    }

    public bool IsTransactionRunning()
    {
        return Database.CurrentTransaction != null;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        await Database.RollbackTransactionAsync(ct);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderSphereDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}