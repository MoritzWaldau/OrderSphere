namespace OrderSphere.BuildingBlocks.EventBus.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public string Type { get; init; } = "";
    public string Content { get; init; } = "";
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }

    /// <summary>
    /// W3C traceparent captured when the row was written, so the asynchronously dispatched
    /// publish joins the originating trace. Null when no trace context was active.
    /// </summary>
    public string? TraceParent { get; init; }

    public const int MaxRetries = 10;
}
