using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Dlq;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.EventBus.AzureServiceBus.Tests.Dlq.TestSupport;

/// <summary>Configurable <see cref="IDlqAdmin"/> fake for exercising <c>DlqEndpointExtensions</c>
/// without a real Service Bus connection.</summary>
internal sealed class StubDlqAdmin : IDlqAdmin
{
    public IReadOnlyList<string> OwnedQueues { get; init; } = [];

    public Func<CancellationToken, Task<Result<IReadOnlyList<DlqQueueDepth>>>>? OnGetDepths { get; init; }
    public Func<string, int, CancellationToken, Task<Result<IReadOnlyList<DeadLetterMessage>>>>? OnPeek { get; init; }
    public Func<string, int, CancellationToken, Task<Result<DlqReplayReport>>>? OnReplay { get; init; }

    public Task<Result<IReadOnlyList<DlqQueueDepth>>> GetDepthsAsync(CancellationToken ct = default) =>
        OnGetDepths is not null ? OnGetDepths(ct) : throw new NotSupportedException();

    public Task<Result<IReadOnlyList<DeadLetterMessage>>> PeekAsync(string queue, int max, CancellationToken ct = default) =>
        OnPeek is not null ? OnPeek(queue, max, ct) : throw new NotSupportedException();

    public Task<Result<DlqReplayReport>> ReplayAsync(string queue, int max, CancellationToken ct = default) =>
        OnReplay is not null ? OnReplay(queue, max, ct) : throw new NotSupportedException();
}
