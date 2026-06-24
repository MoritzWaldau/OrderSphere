using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using NSubstitute;
using OrderSphere.Mcp.Server.Gateway;
using Xunit;

namespace OrderSphere.Mcp.Tests;

// Boots the MCP server in-process and drives it over the real MCP client/transport.
// Unlike the tool unit tests (which call static methods), this proves the actual
// wiring: tool discovery, the Streamable-HTTP transport, and DI of the gateway.
public sealed class McpServerIntegrationTests(McpServerFactory factory)
    : IClassFixture<McpServerFactory>
{
    [Fact]
    public async Task ListTools_ExposesPublicAndUserScopedTools()
    {
        await using var client = await CreateClientAsync();

        var tools = await client.ListToolsAsync();
        var names = tools.Select(t => t.Name).ToList();

        names.Should().Contain(["search_products", "get_product", "list_categories"]);
        names.Should().Contain(["get_my_orders", "get_my_cart", "get_payment_status",
            "list_my_addresses", "get_my_profile", "validate_coupon"]);
        names.Should().Contain("add_to_cart");
    }

    [Fact]
    public async Task ListTools_AnnotatesReadOnlyToolsCorrectly()
    {
        await using var client = await CreateClientAsync();

        var tools = await client.ListToolsAsync();
        var readOnlyTools = tools.Where(t => t.Name != "add_to_cart").ToList();

        // All read-only tools must carry the expected hints.
        readOnlyTools.Should().AllSatisfy(t =>
        {
            var annotations = t.ProtocolTool.Annotations;
            annotations.Should().NotBeNull($"tool '{t.Name}' must carry annotations");
            annotations!.ReadOnlyHint.Should().BeTrue($"tool '{t.Name}' must be read-only");
            annotations.DestructiveHint.Should().BeFalse();
            annotations.IdempotentHint.Should().BeTrue();
            annotations.OpenWorldHint.Should().BeFalse();
        });

        // add_to_cart is the only non-read-only tool.
        var writeAnnotations = tools.Single(t => t.Name == "add_to_cart").ProtocolTool.Annotations;
        writeAnnotations.Should().NotBeNull();
        writeAnnotations!.ReadOnlyHint.Should().BeFalse();
        writeAnnotations.DestructiveHint.Should().BeFalse();
        writeAnnotations.IdempotentHint.Should().BeFalse();
    }

    [Fact]
    public async Task CallSearchProducts_ExecutesToolAndReturnsGatewayData()
    {
        await using var client = await CreateClientAsync();

        var result = await client.CallToolAsync(
            "search_products",
            new Dictionary<string, object?> { ["query"] = "trail" });

        result.IsError.Should().NotBe(true);
        var text = string.Concat(result.Content.OfType<TextContentBlock>().Select(b => b.Text));
        text.Should().Contain("Trail Runner");
    }

    private async Task<McpClient> CreateClientAsync()
    {
        var http = factory.CreateClient();
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(http.BaseAddress!, "mcp") },
            http);
        return await McpClient.CreateAsync(transport);
    }
}

// Hosts the MCP server with the API-gateway client replaced by a stub, so no real
// downstream services are needed.
public sealed class McpServerFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IOrderSphereGateway>();
            services.AddSingleton(CreateStubGateway());
        });
    }

    private static IOrderSphereGateway CreateStubGateway()
    {
        var gateway = Substitute.For<IOrderSphereGateway>();

        gateway.GetProductsAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<decimal?>(), Arg.Any<decimal?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProductDto>(
            [
                new ProductDto(Guid.NewGuid(), "Trail Runner X1", "trail-runner-x1",
                    "Lightweight trail shoe", 99m, 5, Guid.NewGuid(), "Shoes", "SKU-1", null, true)
            ], 1, 1, 50));

        gateway.GetCategoriesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CategoryDto>([], 0, 1, 50));

        gateway.GetProductBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ProductDto?)null);

        gateway.AddToCartAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new CartMutationResult(true, null));

        return gateway;
    }
}
