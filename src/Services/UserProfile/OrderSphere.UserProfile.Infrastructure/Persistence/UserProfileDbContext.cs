using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Extensions;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.UserProfile.Domain.Entities;

namespace OrderSphere.UserProfile.Infrastructure.Persistence;

public sealed class UserProfileDbContext(
    DbContextOptions<UserProfileDbContext> options,
    IPublisher publisher) : DbContext(options)
{
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<SavedAddress> SavedAddresses => Set<SavedAddress>();

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
        configurationBuilder.Properties<CustomerProfileId>().HaveConversion<CustomerProfileIdConverter>();
        configurationBuilder.Properties<SavedAddressId>().HaveConversion<SavedAddressIdConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserProfileDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
