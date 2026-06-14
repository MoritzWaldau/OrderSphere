using System.Diagnostics.Metrics;

namespace OrderSphere.Ordering.Worker;

/// <summary>
/// Order lifecycle business metrics, emitted by the Service Bus consumers. Meter name "OrderSphere"
/// is registered once in ServiceDefaults via <c>AddMeter("OrderSphere")</c>.
/// </summary>
internal static class OrderingMetrics
{
    private static readonly Meter Meter = new("OrderSphere");

    public static readonly Counter<long> OrdersPlaced = Meter.CreateCounter<long>(
        "ordersphere.orders.placed",
        unit: "{order}",
        description: "Orders created from an accepted checkout.");

    public static readonly Counter<long> OrdersConfirmed = Meter.CreateCounter<long>(
        "ordersphere.orders.confirmed",
        unit: "{order}",
        description: "Orders confirmed after a successful payment.");

    public static readonly Counter<long> OrdersCancelled = Meter.CreateCounter<long>(
        "ordersphere.orders.cancelled",
        unit: "{order}",
        description: "Orders cancelled after a failed payment.");
}
