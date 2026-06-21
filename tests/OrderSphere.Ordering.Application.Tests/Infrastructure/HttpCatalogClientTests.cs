using System.Net;
using OrderSphere.Ordering.Infrastructure.CatalogClient;

namespace OrderSphere.Ordering.Application.Tests.Infrastructure;

public sealed class HttpCatalogClientTests
{
    private static (HttpCatalogClient Client, FakeHttpHandler Handler) Build(
        HttpStatusCode status, object? body = null)
    {
        var handler = new FakeHttpHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://catalog") };
        return (new HttpCatalogClient(http, Substitute.For<ILogger<HttpCatalogClient>>()), handler);
    }


    [Fact]
    public async Task GetProductById_ReturnsProduct_OnSuccess()
    {
        var id = Guid.NewGuid();
        var body = new CatalogProductInfo(id, "Widget", 9.99m, 10, true);
        var (client, handler) = Build(HttpStatusCode.OK, body);

        var result = await client.GetProductByIdAsync(id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(id);
        result.Value.Name.Should().Be("Widget");
        handler.LastRequest!.RequestUri!.PathAndQuery.Should().Be($"/internal/products/{id}");
    }

    [Fact]
    public async Task GetProductById_ReturnsFailure_OnNonSuccess()
    {
        var (client, _) = Build(HttpStatusCode.NotFound);

        var result = await client.GetProductByIdAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Catalog.ProductNotFound");
    }


    [Fact]
    public async Task GetProductNames_EmptyList_SkipsHttpCall_ReturnsEmpty()
    {
        var (client, handler) = Build(HttpStatusCode.OK);

        var result = await client.GetProductNamesByIdsAsync([]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        handler.LastRequest.Should().BeNull();
    }

    [Fact]
    public async Task GetProductNames_BuildsQueryString_ForMultipleIds()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var body = ids.ToDictionary(id => id, id => $"Product-{id}");
        var (client, handler) = Build(HttpStatusCode.OK, body);

        await client.GetProductNamesByIdsAsync(ids);

        var query = handler.LastRequest!.RequestUri!.PathAndQuery;
        query.Should().StartWith("/internal/products/names?");
        query.Should().Contain($"ids={ids[0]}").And.Contain($"ids={ids[1]}");
    }


    [Fact]
    public async Task DecrementStock_UsesCorrectEndpoint_OnSuccess()
    {
        var productId = Guid.NewGuid();
        var (client, handler) = Build(HttpStatusCode.OK);

        var result = await client.DecrementStockAsync(productId, 3);

        result.IsSuccess.Should().BeTrue();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.PathAndQuery
            .Should().Be($"/internal/products/{productId}/decrement-stock");
    }

    [Fact]
    public async Task DecrementStock_ReturnsFailure_OnServerError()
    {
        var (client, _) = Build(HttpStatusCode.InternalServerError);

        var result = await client.DecrementStockAsync(Guid.NewGuid(), 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Catalog.StockDecrement");
    }


    [Fact]
    public async Task RestoreStock_UsesCorrectEndpoint_OnSuccess()
    {
        var productId = Guid.NewGuid();
        var (client, handler) = Build(HttpStatusCode.OK);

        var result = await client.RestoreStockAsync(productId, 2);

        result.IsSuccess.Should().BeTrue();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.PathAndQuery
            .Should().Be($"/internal/products/{productId}/restore-stock");
    }


    [Fact]
    public async Task ReserveStock_ReturnsSuccess_OnOk()
    {
        var correlationId = Guid.NewGuid();
        var items = new[] { new ReservationItem(Guid.NewGuid(), 2) };
        var (client, handler) = Build(HttpStatusCode.OK);

        var result = await client.ReserveStockAsync(correlationId, items);

        result.IsSuccess.Should().BeTrue();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.PathAndQuery.Should().Be("/internal/reservations");
    }

    [Fact]
    public async Task ReserveStock_ReturnsConflict_OnHttp409()
    {
        var (client, _) = Build(HttpStatusCode.Conflict);

        var result = await client.ReserveStockAsync(Guid.NewGuid(), []);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Catalog.InsufficientStock");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }


    [Fact]
    public async Task ConfirmReservation_UsesCorrectEndpoint()
    {
        var correlationId = Guid.NewGuid();
        var (client, handler) = Build(HttpStatusCode.OK);

        var result = await client.ConfirmReservationAsync(correlationId);

        result.IsSuccess.Should().BeTrue();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.PathAndQuery
            .Should().Be($"/internal/reservations/{correlationId}/confirm");
    }


    [Fact]
    public async Task ReleaseReservation_UsesCorrectEndpoint()
    {
        var correlationId = Guid.NewGuid();
        var (client, handler) = Build(HttpStatusCode.OK);

        var result = await client.ReleaseReservationAsync(correlationId);

        result.IsSuccess.Should().BeTrue();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.PathAndQuery
            .Should().Be($"/internal/reservations/{correlationId}/release");
    }
}

