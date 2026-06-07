using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using OrderSphere.Mcp.Server.Gateway;
using OrderSphere.Mcp.Server.Tools;
using Xunit;

namespace OrderSphere.Mcp.Tests;

public sealed class OrderingToolsTests
{
    [Fact]
    public async Task GetMyOrders_ReturnsSummary_SortedNewestFirst()
    {
        var older = new OrderDto(Guid.NewGuid(), Guid.NewGuid(), "Shipped", "Card", "TRK1",
            new OrderShippingAddressDto("A", "B", "S", "C", "1", "DE"),
            [new OrderLineDto(Guid.NewGuid(), "P", 1, 10m)], 10m, DateTime.UtcNow.AddDays(-2));
        var newer = older with { Id = Guid.NewGuid(), Status = "Placed", CreatedAt = DateTime.UtcNow };

        var gateway = Substitute.For<IOrderSphereGateway>();
        gateway.GetMyOrdersAsync(Arg.Any<CancellationToken>()).Returns([older, newer]);

        var json = await OrderingTools.GetMyOrdersAsync(gateway);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("count").GetInt32().Should().Be(2);
        doc.RootElement.GetProperty("orders")[0].GetProperty("status")
            .GetString().Should().Be("Placed");
    }

    [Fact]
    public async Task GetOrderStatus_ReturnsMessage_WhenMissing()
    {
        var id = Guid.NewGuid();
        var gateway = Substitute.For<IOrderSphereGateway>();
        gateway.GetOrderAsync(id, Arg.Any<CancellationToken>()).Returns((OrderDto?)null);

        var result = await OrderingTools.GetOrderStatusAsync(gateway, id);

        result.Should().Contain(id.ToString());
    }
}
