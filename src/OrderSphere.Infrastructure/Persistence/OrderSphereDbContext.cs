using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OrderSphere.Application.Abstraction;
using OrderSphere.Domain.Entities;
using OrderSphere.Infrastructure.Outbox;

namespace OrderSphere.Infrastructure.Persistence;

public sealed class OrderSphereDbContext(DbContextOptions<OrderSphereDbContext> options)
    : DbContext(options), IDbContext
{
    // Phase 2: kept for EF model consistency — data exists in monolith DB pending migration to Catalog service.
    // Phase 3+: remove these DbSets and drop the tables via an EF migration.
    internal DbSet<Product> Products => Set<Product>();
    internal DbSet<Category> Categories => Set<Category>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    private IDbContextTransaction? dbContextTransaction = null;

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (dbContextTransaction != null)
            throw new InvalidOperationException("A transaction is already active");

        dbContextTransaction = await Database.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (dbContextTransaction == null)
            throw new InvalidOperationException("No active transaction");

        try
        {
            await SaveChangesAsync(ct);
            await dbContextTransaction.CommitAsync(ct);
        }
        catch
        {
            // Dispose the transaction handle so RollbackAsync called by the
            // handler's catch block can start a fresh one.  Re-throw so the
            // caller is aware the commit failed.
            await dbContextTransaction.DisposeAsync();
            dbContextTransaction = null;
            throw;
        }

        await dbContextTransaction.DisposeAsync();
        dbContextTransaction = null;
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