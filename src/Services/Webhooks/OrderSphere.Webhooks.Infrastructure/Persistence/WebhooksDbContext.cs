using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.EventBus.Inbox;
using OrderSphere.Webhooks.Domain.Entities;

namespace OrderSphere.Webhooks.Infrastructure.Persistence;

public sealed class WebhooksDbContext(DbContextOptions<WebhooksDbContext> options) : DbContext(options)
{
    public DbSet<WebhookSubscription> Subscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDelivery> Deliveries => Set<WebhookDelivery>();
    internal DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WebhooksDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
