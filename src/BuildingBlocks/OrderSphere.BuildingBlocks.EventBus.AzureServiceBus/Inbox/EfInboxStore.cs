using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.EventBus.Inbox;

namespace OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Inbox;

public sealed class EfInboxStore<TContext>(TContext context) : IInboxStore
    where TContext : DbContext
{
    public async Task<bool> HasBeenProcessedAsync(Guid eventId, CancellationToken ct = default)
    {
        return await context.Set<InboxMessage>()
            .AnyAsync(m => m.EventId == eventId, ct);
    }

    public async Task MarkAsProcessedAsync(Guid eventId, string eventType, CancellationToken ct = default)
    {
        context.Set<InboxMessage>().Add(new InboxMessage
        {
            EventId = eventId,
            EventType = eventType,
            ProcessedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync(ct);
    }
}
