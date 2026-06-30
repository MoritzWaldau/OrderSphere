using System.Diagnostics.Metrics;

namespace OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Dlq;

/// <summary>
/// Dead-letter depth metric. Meter name "OrderSphere" is registered once in ServiceDefaults via
/// <c>AddMeter("OrderSphere")</c>. The gauge reads the cached snapshot only — the async polling is
/// done by <see cref="DlqDepthMonitor"/>, never inside the observable callback.
/// </summary>
internal static class DlqMetrics
{
    private static readonly Meter Meter = new("OrderSphere");

    public static void Register(DlqDepthCache cache) =>
        Meter.CreateObservableGauge(
            "ordersphere.dlq.depth",
            () => cache.Snapshot().Select(static entry =>
                new Measurement<int>(entry.Value, new KeyValuePair<string, object?>("queue", entry.Key))),
            unit: "{message}",
            description: "Dead-letter queue depth per queue (capped peek count).");
}
