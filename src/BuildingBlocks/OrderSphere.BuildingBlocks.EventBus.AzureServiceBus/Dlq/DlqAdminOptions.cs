namespace OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Dlq;

/// <summary>
/// Per-host configuration for the DLQ admin surface. Each worker registers the queues it owns;
/// the admin refuses to touch any queue outside this allow-list so one service cannot read or
/// re-drive another service's dead-letter queue.
/// </summary>
public sealed class DlqAdminOptions
{
    /// <summary>Queue names this host consumes and is therefore allowed to inspect/replay.</summary>
    public IReadOnlyList<string> OwnedQueues { get; init; } = [];

    /// <summary>Upper bound on messages peeked or counted in a single call (keeps depth cheap).</summary>
    public int PeekCap { get; init; } = 100;

    /// <summary>Upper bound on messages re-driven in a single replay call.</summary>
    public int ReplayBatchLimit { get; init; } = 50;

    /// <summary>How often the background monitor refreshes the depth gauge.</summary>
    public TimeSpan DepthPollInterval { get; init; } = TimeSpan.FromSeconds(30);
}
