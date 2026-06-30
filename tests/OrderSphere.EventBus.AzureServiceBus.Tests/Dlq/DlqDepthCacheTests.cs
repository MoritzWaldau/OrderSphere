using FluentAssertions;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Dlq;
using Xunit;

namespace OrderSphere.EventBus.AzureServiceBus.Tests.Dlq;

public sealed class DlqDepthCacheTests
{
    [Fact]
    public void Snapshot_WhenEmpty_ReturnsEmpty()
    {
        var cache = new DlqDepthCache();

        cache.Snapshot().Should().BeEmpty();
    }

    [Fact]
    public void Set_ThenSnapshot_ReturnsTheStoredDepth()
    {
        var cache = new DlqDepthCache();

        cache.Set("orders", 3);

        cache.Snapshot().Should().ContainKey("orders").WhoseValue.Should().Be(3);
    }

    [Fact]
    public void Set_CalledTwiceForSameQueue_OverwritesThePreviousDepth()
    {
        var cache = new DlqDepthCache();

        cache.Set("orders", 3);
        cache.Set("orders", 7);

        cache.Snapshot()["orders"].Should().Be(7);
    }

    [Fact]
    public void Set_ForDifferentQueues_TracksEachIndependently()
    {
        var cache = new DlqDepthCache();

        cache.Set("orders", 3);
        cache.Set("payment-requests", 0);

        cache.Snapshot().Should().BeEquivalentTo(new Dictionary<string, int>
        {
            ["orders"] = 3,
            ["payment-requests"] = 0
        });
    }
}
