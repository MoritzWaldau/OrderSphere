using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OrderSphere.Application.Abstraction;
using OrderSphere.Domain.Entities;

namespace OrderSphere.Infrastructure.Persistence;

public sealed class OrderSphereDbContext(DbContextOptions<OrderSphereDbContext> options) : DbContext(options), IDbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

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