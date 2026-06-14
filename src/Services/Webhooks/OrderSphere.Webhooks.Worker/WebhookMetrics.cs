using System.Diagnostics.Metrics;

namespace OrderSphere.Webhooks.Worker;

/// <summary>
/// Outbound webhook delivery metrics. Meter name "OrderSphere" is registered once in
/// ServiceDefaults via <c>AddMeter("OrderSphere")</c>.
/// </summary>
internal static class WebhookMetrics
{
    private static readonly Meter Meter = new("OrderSphere");

    public static readonly Counter<long> Dispatched = Meter.CreateCounter<long>(
        "ordersphere.webhook.dispatched",
        unit: "{delivery}",
        description: "Webhook deliveries that received a 2xx response.");

    public static readonly Counter<long> Failed = Meter.CreateCounter<long>(
        "ordersphere.webhook.failed",
        unit: "{delivery}",
        description: "Webhook deliveries that failed (non-2xx or transport error).");
}
