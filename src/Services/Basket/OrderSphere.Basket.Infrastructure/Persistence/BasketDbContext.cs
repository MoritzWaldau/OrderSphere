using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Basket.Application.Abstractions;
using OrderSphere.Basket.Domain.Entities;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Extensions;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.BuildingBlocks.ValueObjects;

namespace OrderSphere.Basket.Infrastructure.Persistence;

public sealed class BasketDbContext(
    DbContextOptions<BasketDbContext> options,
    IPublisher publisher) : DbContext(options), IBasketDbContext
{
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.ApplyAuditFields();

        var events = ChangeTracker.Entries()
            .Select(e => e.Entity)
            .OfType<IHasDomainEvents>()
            .SelectMany(e => e.PopDomainEvents())
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var @event in events)
            await publisher.Publish(@event, cancellationToken);

        return result;
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<CartId>().HaveConversion<CartIdConverter>();
        configurationBuilder.Properties<CartItemId>().HaveConversion<CartItemIdConverter>();
        configurationBuilder.Properties<CustomerId>().HaveConversion<CustomerIdConverter>();
        configurationBuilder.Properties<ProductId>().HaveConversion<ProductIdConverter>();
        configurationBuilder.Properties<Quantity>().HaveConversion<QuantityConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BasketDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
