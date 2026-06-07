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

    [Fact]
    public async Task SearchProducts_FiltersByQuery_AndRespectsMaxResults()
    {
        var gateway = Substitute.For<IOrderSphereGateway>();
        gateway.GetProductsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductDto>(
            [
                Product("Trail Runner X1"),
                Product("Trail Runner X2"),
                Product("Winter Jacket", category: "Outerwear")
            ], 3, 1, 50));

        var json = await CatalogTools.SearchProductsAsync(gateway, "trail", maxResults: 1);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("count").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("products")[0].GetProperty("name")
            .GetString().Should().StartWith("Trail Runner");
    }

    [Fact]
    public async Task SearchProducts_ExcludesInactiveProducts()
    {
        var inactive = Product("Hidden Item") with { IsActive = false };
        var gateway = Substitute.For<IOrderSphereGateway>();
        gateway.GetProductsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductDto>([inactive], 1, 1, 50));

        var json = await CatalogTools.SearchProductsAsync(gateway, "hidden");

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("count").GetInt32().Should().Be(0);
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
