using OrderSphere.Domain.Outbox;

namespace OrderSphere.Application.Repositories;

public interface IOutboxRepository
{
    Task AddAsync(OutboxEvent evt, CancellationToken ct = default);
    Task<List<OutboxEvent>> GetUnprocessedEventsAsync(int limit, CancellationToken ct = default);
    Task MarkProcessedAsync(OutboxEvent evt, CancellationToken ct = default);
}
