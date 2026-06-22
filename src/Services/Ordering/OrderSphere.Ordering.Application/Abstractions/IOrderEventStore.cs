using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Domain.Entities;

namespace OrderSphere.Ordering.Application.Abstractions;

/// <summary>
/// Persistence for the event-sourced <see cref="Order"/> aggregate. Loads rebuild the aggregate
/// from its stream; appends stage the aggregate's uncommitted events and the synchronous read
/// projection into the unit of work. Neither operation calls <c>SaveChanges</c> — the caller
/// commits events, projection, outbox, and inbox together so the write is atomic.
/// </summary>
public interface IOrderEventStore
{
    /// <summary>Rebuilds the aggregate from its event stream, or null when no stream exists.</summary>
    Task<Order?> LoadAsync(OrderId id, CancellationToken ct = default);

    /// <summary>
    /// Stages the aggregate's uncommitted events and updates the read projection in the change
    /// tracker. Concurrent appends to the same stream collide on the (stream, version) key at
    /// commit time, surfacing as a unique-constraint violation for the caller to treat as a race.
    /// </summary>
    Task AppendAsync(Order order, CancellationToken ct = default);
}
