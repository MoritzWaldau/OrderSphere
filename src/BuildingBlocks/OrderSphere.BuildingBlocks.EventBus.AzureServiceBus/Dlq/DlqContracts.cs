namespace OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Dlq;

/// <summary>A single dead-lettered message, projected for admin inspection (body is truncated).</summary>
public sealed record DeadLetterMessage(
    string MessageId,
    long SequenceNumber,
    string? CorrelationId,
    string? Subject,
    string? DeadLetterReason,
    string? DeadLetterErrorDescription,
    DateTimeOffset EnqueuedTime,
    string BodyPreview);

/// <summary>Dead-letter depth for one queue. <see cref="Capped"/> is true when the count hit the peek cap.</summary>
public sealed record DlqQueueDepth(string Queue, int Depth, bool Capped);

/// <summary>Outcome of a replay call: how many dead-lettered messages were re-driven to the main queue.</summary>
public sealed record DlqReplayReport(string Queue, int Replayed);
