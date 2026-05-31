using Microsoft.EntityFrameworkCore;
using OrderSphere.Webhooks.Domain.Entities;

namespace OrderSphere.Webhooks.Application.Abstractions;

public interface IWebhooksDbContext
{
    DbSet<WebhookSubscription> Subscriptions { get; }
    DbSet<WebhookDelivery> Deliveries { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
