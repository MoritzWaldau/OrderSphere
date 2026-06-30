using System.Collections.Concurrent;

namespace OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Dlq;

/// <summary>
/// Thread-safe cache of the last observed dead-letter depth per queue. The background
/// <see cref="DlqDepthMonitor"/> writes it; the <c>ordersphere.dlq.depth</c> ObservableGauge reads
/// it synchronously. The gauge must never call Service Bus directly — it only reads this snapshot.
/// </summary>
public sealed class DlqDepthCache
{
    private readonly ConcurrentDictionary<string, int> _depths = new();

    public void Set(string queue, int depth) => _depths[queue] = depth;

    public IReadOnlyDictionary<string, int> Snapshot() => _depths;
}
