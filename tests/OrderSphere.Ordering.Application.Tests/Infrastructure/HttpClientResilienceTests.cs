using OrderSphere.Ordering.Infrastructure.CatalogClient;

namespace OrderSphere.Ordering.Application.Tests.Infrastructure;

/// <summary>
/// Transport-failure (catch) paths of the Ordering HTTP clients. Read operations that feed
/// a degraded-but-usable fallback return an empty success; everything else fails closed with
/// a "Catalog.Unavailable" / "Basket.Unavailable" error.
/// </summary>
public sealed class HttpClientResilienceTests
{
    private static HttpCatalogClient Catalog() =>
        new(new HttpClient(new ThrowingHttpHandler()) { BaseAddress = new Uri("http://catalog") },
            Substitute.For<ILogger<HttpCatalogClient>>());

    private static HttpBasketClient Basket() =>
        new(new HttpClient(new ThrowingHttpHandler()) { BaseAddress = new Uri("http://basket") },
            Substitute.For<ILogger<HttpBasketClient>>());


    [Fact]
    public async Task GetProductById_TransportError_ReturnsUnavailable()
    {
        var result = await Catalog().GetProductByIdAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Catalog.Unavailable");
    }

    [Fact]
    public async Task GetProductNames_TransportError_DegradesToEmpty()
    {
        var result = await Catalog().GetProductNamesByIdsAsync([Guid.NewGuid()]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task DecrementStock_TransportError_ReturnsUnavailable()
        => (await Catalog().DecrementStockAsync(Guid.NewGuid(), 1))
            .Error.Code.Should().Be("Catalog.Unavailable");

    [Fact]
    public async Task RestoreStock_TransportError_ReturnsUnavailable()
        => (await Catalog().RestoreStockAsync(Guid.NewGuid(), 1))
            .Error.Code.Should().Be("Catalog.Unavailable");

    [Fact]
    public async Task ReserveStock_TransportError_ReturnsUnavailable()
        => (await Catalog().ReserveStockAsync(Guid.NewGuid(), [new ReservationItem(Guid.NewGuid(), 1)]))
            .Error.Code.Should().Be("Catalog.Unavailable");

    [Fact]
    public async Task ConfirmReservation_TransportError_ReturnsUnavailable()
        => (await Catalog().ConfirmReservationAsync(Guid.NewGuid()))
            .Error.Code.Should().Be("Catalog.Unavailable");

    [Fact]
    public async Task ReleaseReservation_TransportError_ReturnsUnavailable()
        => (await Catalog().ReleaseReservationAsync(Guid.NewGuid()))
            .Error.Code.Should().Be("Catalog.Unavailable");


    [Fact]
    public async Task GetCart_TransportError_ReturnsUnavailable()
    {
        var result = await Basket().GetCartAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Basket.Unavailable");
    }

    [Fact]
    public async Task ClearCartItems_TransportError_ReturnsUnavailable()
        => (await Basket().ClearCartItemsAsync(Guid.NewGuid()))
            .Error.Code.Should().Be("Basket.Unavailable");
}

