using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using OrderSphere.Mcp.Server.Gateway;
using OrderSphere.Mcp.Server.Tools;
using Xunit;

namespace OrderSphere.Mcp.Tests;

public sealed class BasketWriteToolsTests
{
    private static ProductDto ActiveProduct(int stock = 10) => new(
        Guid.NewGuid(), "Trail Runner X1", "trail-runner-x1",
        "Lightweight trail shoe", 99.99m, stock,
        Guid.NewGuid(), "Shoes", "TR-X1", null, IsActive: true);

    [Fact]
    public async Task AddToCart_ReturnsAuthRequired_WhenAnonymous()
    {
        var gateway = Substitute.For<IOrderSphereGateway>();

        var result = await BasketWriteTools.AddToCartAsync(
            FakeCaller.Anonymous, gateway, "trail-runner-x1", 1);

        result.Should().Be(UserToolGuard.AuthRequired);
        await gateway.DidNotReceive().GetProductBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await gateway.DidNotReceive().AddToCartAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddToCart_ReturnsConfirmPayload_WhenNotConfirmed()
    {
        var product = ActiveProduct();
        var gateway = Substitute.For<IOrderSphereGateway>();
        gateway.GetProductBySlugAsync(product.Slug, Arg.Any<CancellationToken>()).Returns(product);

        var result = await BasketWriteTools.AddToCartAsync(
            FakeCaller.Authenticated, gateway, product.Slug, 2, confirmed: false);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("__confirm__").GetString().Should().Be("add_to_cart");
        doc.RootElement.GetProperty("slug").GetString().Should().Be(product.Slug);
        doc.RootElement.GetProperty("quantity").GetInt32().Should().Be(2);
        doc.RootElement.GetProperty("productName").GetString().Should().Be(product.Name);
        doc.RootElement.GetProperty("unitPrice").GetDecimal().Should().Be(product.Price);
        await gateway.DidNotReceive().AddToCartAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddToCart_CallsGateway_AndReturnsSuccess_WhenConfirmed()
    {
        var product = ActiveProduct();
        var gateway = Substitute.For<IOrderSphereGateway>();
        gateway.GetProductBySlugAsync(product.Slug, Arg.Any<CancellationToken>()).Returns(product);
        gateway.AddToCartAsync(product.Id, 1, Arg.Any<CancellationToken>())
            .Returns(new CartMutationResult(true, null));

        var result = await BasketWriteTools.AddToCartAsync(
            FakeCaller.Authenticated, gateway, product.Slug, 1, confirmed: true);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        await gateway.Received(1).AddToCartAsync(product.Id, 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddToCart_ReturnsError_WhenProductNotFound()
    {
        var gateway = Substitute.For<IOrderSphereGateway>();
        gateway.GetProductBySlugAsync("unknown-slug", Arg.Any<CancellationToken>()).Returns((ProductDto?)null);

        var result = await BasketWriteTools.AddToCartAsync(
            FakeCaller.Authenticated, gateway, "unknown-slug", 1);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("error").GetString().Should().Contain("nicht gefunden");
        await gateway.DidNotReceive().AddToCartAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddToCart_ReturnsError_WhenProductIsInactive()
    {
        var product = new ProductDto(Guid.NewGuid(), "Discontinued Shoe", "disc-shoe",
            "Old model", 49.99m, 5, Guid.NewGuid(), "Shoes", "DS-1", null, IsActive: false);
        var gateway = Substitute.For<IOrderSphereGateway>();
        gateway.GetProductBySlugAsync(product.Slug, Arg.Any<CancellationToken>()).Returns(product);

        var result = await BasketWriteTools.AddToCartAsync(
            FakeCaller.Authenticated, gateway, product.Slug, 1);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("error").GetString().Should().Contain("nicht verfügbar");
        await gateway.DidNotReceive().AddToCartAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddToCart_ReturnsError_WhenGatewayReportsInsufficientStock()
    {
        var product = ActiveProduct();
        var gateway = Substitute.For<IOrderSphereGateway>();
        gateway.GetProductBySlugAsync(product.Slug, Arg.Any<CancellationToken>()).Returns(product);
        gateway.AddToCartAsync(product.Id, 99, Arg.Any<CancellationToken>())
            .Returns(new CartMutationResult(false, "insufficient_stock"));

        var result = await BasketWriteTools.AddToCartAsync(
            FakeCaller.Authenticated, gateway, product.Slug, 99, confirmed: true);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("error").GetString().Should().Contain("Lagerbestand");
    }

    [Fact]
    public async Task AddToCart_ReturnsError_WhenQuantityIsZero()
    {
        var gateway = Substitute.For<IOrderSphereGateway>();

        var result = await BasketWriteTools.AddToCartAsync(
            FakeCaller.Authenticated, gateway, "any-slug", 0);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("error").GetString().Should().Contain("mindestens 1");
        await gateway.DidNotReceive().GetProductBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
