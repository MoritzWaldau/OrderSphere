using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.EventBus.Inbox;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Webhooks.Domain.Entities;

namespace OrderSphere.Webhooks.Infrastructure.Persistence;

public sealed class WebhooksDbContext(DbContextOptions<WebhooksDbContext> options) : DbContext(options)
{
    public DbSet<WebhookSubscription> Subscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDelivery> Deliveries => Set<WebhookDelivery>();
    internal DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<WebhookSubscriptionId>().HaveConversion<WebhookSubscriptionIdConverter>();
        configurationBuilder.Properties<WebhookDeliveryId>().HaveConversion<WebhookDeliveryIdConverter>();
        configurationBuilder.Properties<CustomerId>().HaveConversion<CustomerIdConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WebhooksDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
