namespace OrderSphere.Ordering.Domain.Entities;

/// <summary>
/// Denormalised CQRS read-model row: one entry per order status transition, projected
/// asynchronously from <c>OrderStatusChangedIntegrationEvent</c> by the worker's
/// <c>OrderHistoryProjector</c>. Unlike <see cref="OrderStatusHistory"/> — which is an owned
/// collection of the <see cref="Order"/> write aggregate and only reachable by loading the
/// order — this model is a standalone, append-only materialised view. It carries the
/// denormalised <see cref="CustomerEmail"/> so cross-order activity queries (the staff
/// activity feed) never join back to the write tables.
///
/// Not an <c>AuditableEntity</c>: a read-model has no soft-delete semantics and never mutates
/// after insertion. Eventually consistent with the order aggregate (Service Bus latency).
/// </summary>
public sealed class OrderHistoryEntry
{
    // Client-generated v7 (time-ordered) so no store identity is required across providers.
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid OrderId { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string CustomerEmail { get; private set; } = string.Empty;
    public string PreviousStatus { get; private set; } = string.Empty;
    public string NewStatus { get; private set; } = string.Empty;

    /// <summary>When the transition occurred (the originating integration event's timestamp).</summary>
    public DateTime OccurredAt { get; private set; }

    // Parameterless constructor for EF Core materialisation.
    private OrderHistoryEntry() { }

    public static OrderHistoryEntry Record(
        Guid orderId,
        Guid correlationId,
        string customerEmail,
        string previousStatus,
        string newStatus,
        DateTime occurredAt) => new()
        {
            OrderId = orderId,
            CorrelationId = correlationId,
            CustomerEmail = customerEmail,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            OccurredAt = occurredAt
        };
}
