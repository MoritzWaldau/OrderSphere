using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using OrderSphere.Mcp.Server.Gateway;
using OrderSphere.Mcp.Server.Tools;
using Xunit;

namespace OrderSphere.Mcp.Tests;

public sealed class UserScopedToolsTests
{
    [Fact]
    public async Task GetMyCart_ReturnsItems_WithLineAndCartTotals()
    {
        var cart = new CartDto(Guid.NewGuid(),
        [
            new CartItemDto(Guid.NewGuid(), "Trail Runner X1", 100m, 2),
            new CartItemDto(Guid.NewGuid(), "Wool Socks", 15m, 1)
        ]);

        var gateway = Substitute.For<IOrderSphereGateway>();
        gateway.GetMyCartAsync(Arg.Any<CancellationToken>()).Returns(cart);

        var json = await BasketTools.GetMyCartAsync(FakeCaller.Authenticated, gateway);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("itemCount").GetInt32().Should().Be(2);
        doc.RootElement.GetProperty("total").GetDecimal().Should().Be(215m);
    }

    [Fact]
    public async Task GetMyCart_ReturnsMessage_WhenGatewayHasNoCart()
    {
        var gateway = Substitute.For<IOrderSphereGateway>();
        gateway.GetMyCartAsync(Arg.Any<CancellationToken>()).Returns((CartDto?)null);

        var result = await BasketTools.GetMyCartAsync(FakeCaller.Authenticated, gateway);

        result.Should().Contain("No cart available");
    }

    [Fact]
    public async Task GetMyCart_ReturnsAuthRequired_WhenAnonymous_AndDoesNotCallGateway()
    {
        var gateway = Substitute.For<IOrderSphereGateway>();

        var result = await BasketTools.GetMyCartAsync(FakeCaller.Anonymous, gateway);

        result.Should().Be(UserToolGuard.AuthRequired);
        await gateway.DidNotReceive().GetMyCartAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPaymentStatus_ReturnsMessage_WhenMissing()
    {
        var orderId = Guid.NewGuid();
        var gateway = Substitute.For<IOrderSphereGateway>();
        gateway.GetPaymentByOrderAsync(orderId, Arg.Any<CancellationToken>()).Returns((PaymentDto?)null);

        var result = await PaymentTools.GetPaymentStatusAsync(FakeCaller.Authenticated, gateway, orderId);

        result.Should().Contain(orderId.ToString());
    }

    [Fact]
    public async Task GetPaymentStatus_ReturnsAuthRequired_WhenAnonymous()
    {
        var gateway = Substitute.For<IOrderSphereGateway>();

        var result = await PaymentTools.GetPaymentStatusAsync(FakeCaller.Anonymous, gateway, Guid.NewGuid());

        result.Should().Be(UserToolGuard.AuthRequired);
        await gateway.DidNotReceive().GetPaymentByOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPaymentStatus_OmitsInternalId_WhenPresent()
    {
        var orderId = Guid.NewGuid();
        var payment = new PaymentDto(Guid.NewGuid(), orderId, 215m, "EUR", "Card",
            "Succeeded", "txn_123", null, DateTime.UtcNow);

        var gateway = Substitute.For<IOrderSphereGateway>();
        gateway.GetPaymentByOrderAsync(orderId, Arg.Any<CancellationToken>()).Returns(payment);

        var json = await PaymentTools.GetPaymentStatusAsync(FakeCaller.Authenticated, gateway, orderId);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Succeeded");
        doc.RootElement.TryGetProperty("transactionId", out _).Should().BeFalse();
    }

    [Fact]
    public async Task ListMyAddresses_FlagsDefault_AndCounts()
    {
        var addresses = new List<AddressDto>
        {
            new(Guid.NewGuid(), "Home", "Max", "Muster", "Hauptstr. 1", "Berlin", "10115", "DE", true),
            new(Guid.NewGuid(), "Work", "Max", "Muster", "Bürogasse 9", "Berlin", "10117", "DE", false)
        };

        var gateway = Substitute.For<IOrderSphereGateway>();
        gateway.GetMyAddressesAsync(Arg.Any<CancellationToken>()).Returns(addresses);

        var json = await ProfileTools.ListMyAddressesAsync(FakeCaller.Authenticated, gateway);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("count").GetInt32().Should().Be(2);
        doc.RootElement.GetProperty("addresses")[0].GetProperty("isDefault")
            .GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ListMyAddresses_ReturnsMessage_WhenEmpty()
    {
        var gateway = Substitute.For<IOrderSphereGateway>();
        gateway.GetMyAddressesAsync(Arg.Any<CancellationToken>()).Returns([]);

        var result = await ProfileTools.ListMyAddressesAsync(FakeCaller.Authenticated, gateway);

        result.Should().Contain("No saved addresses");
    }

    [Fact]
    public async Task ListMyAddresses_ReturnsAuthRequired_WhenAnonymous()
    {
        var gateway = Substitute.For<IOrderSphereGateway>();

        var result = await ProfileTools.ListMyAddressesAsync(FakeCaller.Anonymous, gateway);

        result.Should().Be(UserToolGuard.AuthRequired);
        await gateway.DidNotReceive().GetMyAddressesAsync(Arg.Any<CancellationToken>());
    }
}
