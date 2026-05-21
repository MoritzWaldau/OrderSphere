namespace OrderSphere.Infrastructure.Outbox;

internal sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public string Type { get; init; } = "";
    public string Content { get; init; } = "";
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }

    /// <summary>Number of dispatch attempts so far (including failures).</summary>
    public int RetryCount { get; set; }

    internal const int MaxRetries = 10;
}
