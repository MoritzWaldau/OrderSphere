using System.Net;
using OrderSphere.Ordering.Infrastructure.CatalogClient;

namespace OrderSphere.Ordering.Application.Tests.Infrastructure;

public sealed class HttpBasketClientTests
{
    private static (HttpBasketClient Client, FakeHttpHandler Handler) Build(
        HttpStatusCode status, object? body = null)
    {
        var handler = new FakeHttpHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://basket") };
        return (new HttpBasketClient(http, Substitute.For<ILogger<HttpBasketClient>>()), handler);
    }


    [Fact]
    public async Task GetCart_ReturnsCart_OnSuccess()
    {
        var customerId = Guid.NewGuid();
        var body = new BasketCartInfo(customerId,
            [new BasketCartItemInfo(Guid.NewGuid(), 2)]);
        var (client, handler) = Build(HttpStatusCode.OK, body);

        var result = await client.GetCartAsync(customerId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CustomerId.Should().Be(customerId);
        result.Value.Items.Should().HaveCount(1);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.PathAndQuery.Should().Be($"/internal/cart/{customerId}");
    }

    [Fact]
    public async Task GetCart_ReturnsFailure_OnNonSuccess()
    {
        var (client, _) = Build(HttpStatusCode.NotFound);

        var result = await client.GetCartAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Basket.CartNotFound");
    }


    [Fact]
    public async Task ClearCart_UsesDeleteAndCorrectEndpoint()
    {
        var customerId = Guid.NewGuid();
        var (client, handler) = Build(HttpStatusCode.OK);

        var result = await client.ClearCartItemsAsync(customerId);

        result.IsSuccess.Should().BeTrue();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.PathAndQuery
            .Should().Be($"/internal/cart/{customerId}/items");
    }

    [Fact]
    public async Task ClearCart_ReturnsFailure_OnServerError()
    {
        var (client, _) = Build(HttpStatusCode.InternalServerError);

        var result = await client.ClearCartItemsAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Basket.ClearFailed");
    }
}

