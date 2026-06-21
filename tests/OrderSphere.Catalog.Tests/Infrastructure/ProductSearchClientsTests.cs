using Microsoft.Extensions.Configuration;

namespace OrderSphere.Catalog.Tests.Infrastructure;

/// <summary>
/// Construction-time behaviour of the search client holder. The index enables only when
/// both the Search endpoint and the Foundry embedding endpoint are configured; otherwise
/// the catalog degrades to database search.
/// </summary>
public sealed class ProductSearchClientsTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void Disabled_WhenNeitherEndpointConfigured()
    {
        var clients = new ProductSearchClients(Config(("Search:Endpoint", "")));

        clients.IsEnabled.Should().BeFalse();
        clients.SearchClient.Should().BeNull();
        clients.EmbeddingClient.Should().BeNull();
    }

    [Fact]
    public void Disabled_WhenSearchPresentButFoundryMissing()
    {
        var clients = new ProductSearchClients(Config(
            ("Search:Endpoint", "https://search.search.windows.net"),
            ("Foundry:Endpoint", "")));

        clients.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Enabled_WhenBothEndpointsConfigured()
    {
        var clients = new ProductSearchClients(Config(
            ("Search:Endpoint", "https://search.search.windows.net"),
            ("Foundry:Endpoint", "https://foundry.openai.azure.com")));

        clients.IsEnabled.Should().BeTrue();
        clients.SearchClient.Should().NotBeNull();
        clients.EmbeddingClient.Should().NotBeNull();
    }

    [Fact]
    public void Enabled_WhenSearchProvidedAsConnectionStringForm()
    {
        var clients = new ProductSearchClients(Config(
            ("Search:Endpoint", ""),
            ("ConnectionStrings:search", "Endpoint=https://search.search.windows.net;Key=abc"),
            ("Foundry:Endpoint", "https://foundry.openai.azure.com")));

        clients.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void DefaultIndexName_IsProducts()
    {
        var clients = new ProductSearchClients(Config(("Search:Endpoint", "")));

        clients.IndexName.Should().Be("products");
    }

    [Fact]
    public void CustomIndexName_IsHonored()
    {
        var clients = new ProductSearchClients(Config(
            ("Search:Endpoint", ""),
            ("Search:IndexName", "catalog-products")));

        clients.IndexName.Should().Be("catalog-products");
    }

    [Fact]
    public void Constants_AreExposed()
    {
        ProductSearchClients.EmbeddingDimensions.Should().Be(1536);
        ProductSearchClients.VectorFieldName.Should().Be("contentVector");
    }

    [Fact]
    public async Task EnsureIndexAsync_WhenDisabled_ReturnsWithoutCallingAzure()
    {
        var clients = new ProductSearchClients(Config(("Search:Endpoint", "")));

        // Disabled: the method must early-return rather than dereference a null index client.
        await clients.EnsureIndexAsync(default);
    }
}
