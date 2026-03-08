using Microsoft.EntityFrameworkCore;
using OrderSphere.Application.Repositories;
using OrderSphere.Domain.Outbox;
using OrderSphere.Infrastructure.Persistence;

namespace OrderSphere.Infrastructure.Repositories;

public sealed class OutboxRepository(OrderSphereDbContext Context) : IOutboxRepository
{
    public async Task AddAsync(OutboxEvent evt, CancellationToken ct = default)
        => await Context.OutboxEvents.AddAsync(evt, ct);

    public async Task<List<OutboxEvent>> GetUnprocessedEventsAsync(int limit, CancellationToken ct = default)
        => await Context.OutboxEvents
                    .Where(e => !e.Processed)
                    .OrderBy(e => e.OccurredOn)
                    .Take(limit)
                    .ToListAsync(ct);

    public async Task MarkProcessedAsync(OutboxEvent evt, CancellationToken ct = default)
    {
        evt.Processed = true;
        evt.ProcessedOn = DateTime.UtcNow;
        await Context.SaveChangesAsync(ct);
    }
}
