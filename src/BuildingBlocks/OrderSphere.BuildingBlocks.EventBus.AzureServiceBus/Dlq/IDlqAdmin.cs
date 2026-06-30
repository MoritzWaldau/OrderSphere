using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Dlq;

/// <summary>
/// Read/replay operations over the dead-letter sub-queues this host owns. All operations are
/// guarded by the owned-queue allow-list configured in <see cref="DlqAdminOptions"/>; a request
/// for an unowned queue fails with <see cref="ErrorType.NotFound"/>.
/// </summary>
public interface IDlqAdmin
{
    /// <summary>The queues this host is allowed to inspect and replay.</summary>
    IReadOnlyList<string> OwnedQueues { get; }

    /// <summary>Current dead-letter depth (capped at <see cref="DlqAdminOptions.PeekCap"/>) per owned queue.</summary>
    Task<Result<IReadOnlyList<DlqQueueDepth>>> GetDepthsAsync(CancellationToken ct = default);

    /// <summary>Peeks up to <paramref name="max"/> dead-lettered messages without removing them.</summary>
    Task<Result<IReadOnlyList<DeadLetterMessage>>> PeekAsync(string queue, int max, CancellationToken ct = default);

    /// <summary>
    /// Re-drives up to <paramref name="max"/> dead-lettered messages back onto the main queue.
    /// Each message is cloned (body + application properties incl. <c>traceparent</c> + correlation
    /// preserved) and the dead-letter copy is completed. The original event id is kept, so downstream
    /// inbox dedup still applies.
    /// </summary>
    Task<Result<DlqReplayReport>> ReplayAsync(string queue, int max, CancellationToken ct = default);
}
