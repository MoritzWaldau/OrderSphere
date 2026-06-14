using System.Diagnostics.Metrics;

namespace OrderSphere.BuildingBlocks.EventBus.AzureServiceBus;

/// <summary>
/// Outbox-pipeline health metrics. Meter name "OrderSphere" is registered once in ServiceDefaults
/// via <c>AddMeter("OrderSphere")</c>; this assembly does not reference the domain building block,
/// so the name is used as a literal here.
/// </summary>
internal static class OutboxMetrics
{
    private static readonly Meter Meter = new("OrderSphere");

    public static readonly Counter<long> Published = Meter.CreateCounter<long>(
        "ordersphere.outbox.published",
        unit: "{message}",
        description: "Outbox messages successfully dispatched to the event bus.");

    public static readonly Counter<long> Poison = Meter.CreateCounter<long>(
        "ordersphere.outbox.poison",
        unit: "{message}",
        description: "Outbox messages that permanently failed after the maximum retries.");
}
