using Microsoft.Extensions.Configuration;

namespace OrderSphere.Catalog.Tests.Infrastructure;

/// <summary>
/// Construction-time behaviour of the blob client holder: when it enables itself, what
/// container name it resolves, and how it normalizes the endpoint forms Aspire can inject.
/// Network operations (SAS, upload) are out of scope — they require a live account.
/// </summary>
public sealed class BlobStorageClientsTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void Disabled_WhenNoEndpointConfigured()
    {
        // appsettings ships Blob:Endpoint as "" — whitespace must not enable the client.
        using var clients = new BlobStorageClients(Config(("Blob:Endpoint", "")));

        clients.IsEnabled.Should().BeFalse();
        clients.Container.Should().BeNull();
    }

    [Fact]
    public void Disabled_DefaultContainerName_StillResolved()
    {
        using var clients = new BlobStorageClients(Config(("Blob:Endpoint", "")));

        clients.ContainerName.Should().Be("product-images");
    }

    [Fact]
    public void Enabled_WhenBareEndpointUrlConfigured()
    {
        using var clients = new BlobStorageClients(Config(
            ("Blob:Endpoint", "https://acct.blob.core.windows.net")));

        clients.IsEnabled.Should().BeTrue();
        clients.Container.Should().NotBeNull();
    }

    [Fact]
    public void Enabled_WhenConnectionStringFormProvided_NormalizesEndpoint()
    {
        // Aspire can inject the "BlobEndpoint=...;..." key-value form; it must be normalized.
        using var clients = new BlobStorageClients(Config(
            ("Blob:Endpoint", ""),
            ("ConnectionStrings:images", "BlobEndpoint=https://acct.blob.core.windows.net;")));

        clients.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void CustomContainerName_IsHonored()
    {
        using var clients = new BlobStorageClients(Config(
            ("Blob:Endpoint", "https://acct.blob.core.windows.net"),
            ("Blob:ContainerName", "custom-images")));

        clients.ContainerName.Should().Be("custom-images");
    }
}
