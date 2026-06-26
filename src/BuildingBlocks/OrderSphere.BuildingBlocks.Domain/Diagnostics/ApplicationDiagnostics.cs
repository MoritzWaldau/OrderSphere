using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace OrderSphere.BuildingBlocks.Diagnostics;

/// <summary>
/// Shared diagnostics primitives. The meter name <see cref="MeterName"/> ("OrderSphere") is the
/// single registration handle for every custom OrderSphere metric — registered once in
/// ServiceDefaults via <c>metrics.AddMeter("OrderSphere")</c>. The activity source surfaces a
/// span per MediatR request (CQRS handler) and is registered via
/// <c>tracing.AddSource("OrderSphere.Application")</c>.
/// </summary>
public static class ApplicationDiagnostics
{
    public const string MeterName = "OrderSphere";
    public const string ActivitySourceName = "OrderSphere.Application";

    public static readonly Meter Meter = new(MeterName);
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    /// <summary>MediatR request handler duration in milliseconds, tagged by request and outcome.</summary>
    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "ordersphere.mediatr.request.duration",
        unit: "ms",
        description: "MediatR request handler duration.");

    /// <summary>Advisory agent semantic cache hits.</summary>
    public static readonly Counter<long> AdvisorCacheHits = Meter.CreateCounter<long>(
        "ordersphere.advisor.cache.hits",
        description: "Number of advisory turns served from the semantic cache.");

    /// <summary>Advisory agent semantic cache misses (query forwarded to Foundry).</summary>
    public static readonly Counter<long> AdvisorCacheMisses = Meter.CreateCounter<long>(
        "ordersphere.advisor.cache.misses",
        description: "Number of advisory turns that bypassed the semantic cache.");
}
