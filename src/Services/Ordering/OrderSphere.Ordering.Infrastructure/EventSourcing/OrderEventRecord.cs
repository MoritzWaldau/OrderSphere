namespace OrderSphere.Ordering.Infrastructure.EventSourcing;

/// <summary>
/// One persisted event in an order's stream. The primary key is (<see cref="StreamId"/>,
/// <see cref="Version"/>): a stream is a gap-free 1-based sequence, and the composite key gives
/// optimistic concurrency for free — two writers appending the same next version collide on insert.
/// </summary>
public sealed class OrderEventRecord
{
    /// <summary>The aggregate (order) id this event belongs to.</summary>
    public Guid StreamId { get; set; }

    /// <summary>1-based position of this event within the stream.</summary>
    public int Version { get; set; }

    /// <summary>Discriminator used to deserialize <see cref="Payload"/> back to a concrete event.</summary>
    public string EventType { get; set; } = null!;

    /// <summary>JSON-serialized event body.</summary>
    public string Payload { get; set; } = null!;

    public DateTime OccurredAt { get; set; }
}
