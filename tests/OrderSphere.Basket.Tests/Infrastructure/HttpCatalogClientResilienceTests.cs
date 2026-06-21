using OrderSphere.Basket.Infrastructure.CatalogClient;

namespace OrderSphere.Basket.Tests.Infrastructure;

/// <summary>
/// Transport-failure behaviour: a thrown request must be caught and degraded rather than
/// surfaced. Single-product lookups fail; bulk info lookups degrade to an empty result.
/// </summary>
public sealed class HttpCatalogClientResilienceTests
{
    private static HttpCatalogClient Build() =>
        new(new HttpClient(new ThrowingHttpHandler()) { BaseAddress = new Uri("http://catalog") },
            Substitute.For<ILogger<HttpCatalogClient>>());

    [Fact]
    public async Task GetProductById_TransportError_ReturnsUnavailable()
    {
        var result = await Build().GetProductByIdAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Catalog.Unavailable");
    }

    [Fact]
    public async Task GetProductInfos_TransportError_DegradesToEmpty()
    {
        var result = await Build().GetProductInfosByIdsAsync([Guid.NewGuid()]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
