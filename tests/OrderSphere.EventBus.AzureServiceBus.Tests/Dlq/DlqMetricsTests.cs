using System.Diagnostics.Metrics;
using FluentAssertions;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Dlq;
using Xunit;

namespace OrderSphere.EventBus.AzureServiceBus.Tests.Dlq;

/// <summary>Verifies the <c>ordersphere.dlq.depth</c> gauge reads the cache snapshot, by attaching a
/// <see cref="MeterListener"/> and forcing an observation.</summary>
public sealed class DlqMetricsTests
{
    [Fact]
    public void Register_GaugeObservation_ReportsOneMeasurementPerCachedQueue()
    {
        var cache = new DlqDepthCache();
        cache.Set("orders", 4);
        cache.Set("payment-results", 0);

        var measurements = new List<(string Queue, int Depth)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == "OrderSphere" && instrument.Name == "ordersphere.dlq.depth")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<int>((_, measurement, tags, _) =>
        {
            var queue = tags.ToArray().First(t => t.Key == "queue").Value!.ToString()!;
            measurements.Add((queue, measurement));
        });
        listener.Start();

        DlqMetrics.Register(cache);
        listener.RecordObservableInstruments();

        measurements.Should().Contain(m => m.Queue == "orders" && m.Depth == 4);
        measurements.Should().Contain(m => m.Queue == "payment-results" && m.Depth == 0);
    }
}
