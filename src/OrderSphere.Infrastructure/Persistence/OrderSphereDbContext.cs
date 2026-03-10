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
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    private IDbContextTransaction? dbContextTransaction = null;

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (dbContextTransaction != null)
            throw new InvalidOperationException("No active transaction");

        dbContextTransaction = await Database.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (dbContextTransaction == null)
             throw new InvalidOperationException("No active transaction");

        try
        {
            // Persistiere Änderungen vor dem Commit
            await SaveChangesAsync(ct);
            await dbContextTransaction.CommitAsync(ct);
        }
        finally
        {
            await dbContextTransaction.DisposeAsync();
            dbContextTransaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (dbContextTransaction == null)
            return;

        try
        {
            await dbContextTransaction.RollbackAsync(ct);
        }
        finally
        {
            await dbContextTransaction.DisposeAsync();
            dbContextTransaction = null;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderSphereDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}