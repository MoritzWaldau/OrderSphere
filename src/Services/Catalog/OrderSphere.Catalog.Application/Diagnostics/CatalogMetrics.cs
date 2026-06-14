using System.Diagnostics.Metrics;

namespace OrderSphere.Catalog.Application.Diagnostics;

/// <summary>
/// Catalog cache efficiency metrics. Meter name "OrderSphere" is registered once in ServiceDefaults
/// via <c>AddMeter("OrderSphere")</c>.
/// </summary>
internal static class CatalogMetrics
{
    private static readonly Meter Meter = new("OrderSphere");

    public static readonly Counter<long> CacheHits = Meter.CreateCounter<long>(
        "ordersphere.catalog.cache.hits",
        unit: "{lookup}",
        description: "Catalog reads served from the hybrid cache.");

    public static readonly Counter<long> CacheMisses = Meter.CreateCounter<long>(
        "ordersphere.catalog.cache.misses",
        unit: "{lookup}",
        description: "Catalog reads that fell through to the database.");
}
