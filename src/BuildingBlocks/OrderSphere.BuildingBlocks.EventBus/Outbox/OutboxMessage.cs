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

    public const int MaxRetries = 10;
}
