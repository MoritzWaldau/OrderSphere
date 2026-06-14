using System.Diagnostics.Metrics;

namespace OrderSphere.Payment.Worker;

/// <summary>
/// Payment processing business metrics. Meter name "OrderSphere" is registered once in
/// ServiceDefaults via <c>AddMeter("OrderSphere")</c>.
/// </summary>
internal static class PaymentMetrics
{
    private static readonly Meter Meter = new("OrderSphere");

    public static readonly Counter<long> Processed = Meter.CreateCounter<long>(
        "ordersphere.payments.processed",
        unit: "{payment}",
        description: "Payments processed, tagged by provider and outcome.");

    public static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(
        "ordersphere.payment.duration",
        unit: "ms",
        description: "Payment authorize+capture duration at the provider.");
}
