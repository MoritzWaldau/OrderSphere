using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using OrderSphere.Mcp.Server.Gateway;
using OrderSphere.Mcp.Server.Tools;
using Xunit;

namespace OrderSphere.Mcp.Tests;

public sealed class CatalogToolsTests
{
    private static ProductDto Product(string name, string category = "Shoes", decimal price = 99m, int stock = 5)
        => new(Guid.NewGuid(), name, name.ToLowerInvariant().Replace(' ', '-'),
               $"{name} description", price, stock, Guid.NewGuid(), category, "SKU-1", null, true);

    private static IOrderSphereGateway GatewayReturning(params ProductDto[] products)
    {
        var gateway = Substitute.For<IOrderSphereGateway>();
        gateway.GetProductsAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<decimal?>(), Arg.Any<decimal?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductDto>(products, products.Length, 1, 10));
        return gateway;
    }

    [Fact]
    public async Task SearchProducts_DelegatesFiltersToGateway()
    {
        var gateway = GatewayReturning(Product("Trail Runner X1"));

        await CatalogTools.SearchProductsAsync(
            gateway, "trail", category: "Shoes", minPrice: 50m, maxPrice: 100m, maxResults: 5);

        await gateway.Received(1).GetProductsAsync(
            1, 5, "trail", "Shoes", 50m, 100m, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchProducts_EmptyQuery_PassesNullSearchTerm()
    {
        var gateway = GatewayReturning();

        await CatalogTools.SearchProductsAsync(gateway, "  ", category: "Shoes");

        await gateway.Received(1).GetProductsAsync(
            1, 10, null, "Shoes", null, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchProducts_ShapesResult_WithCountAndTotalMatches()
    {
        var gateway = Substitute.For<IOrderSphereGateway>();
        gateway.GetProductsAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<decimal?>(), Arg.Any<decimal?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductDto>([Product("Trail Runner X1")], 37, 1, 1));

        var json = await CatalogTools.SearchProductsAsync(gateway, "trail", maxResults: 1);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("count").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("totalMatches").GetInt32().Should().Be(37);
        doc.RootElement.GetProperty("products")[0].GetProperty("name")
            .GetString().Should().Be("Trail Runner X1");
    }

    [Fact]
    public async Task GetProduct_ReturnsMessage_WhenNotFound()
    {
        var gateway = Substitute.For<IOrderSphereGateway>();
        gateway.GetProductBySlugAsync("missing", Arg.Any<CancellationToken>())
            .Returns((ProductDto?)null);

        var result = await CatalogTools.GetProductAsync(gateway, "missing");

        result.Should().Contain("missing");
    }
}
