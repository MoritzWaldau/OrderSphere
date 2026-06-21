using System.Net;
using OrderSphere.Basket.Infrastructure.CatalogClient;

namespace OrderSphere.Basket.Tests.Infrastructure;

public sealed class HttpCatalogClientTests
{
    private static (HttpCatalogClient Client, FakeHttpHandler Handler) Build(
        HttpStatusCode status, object? body = null)
    {
        var handler = new FakeHttpHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://catalog") };
        return (new HttpCatalogClient(http, Substitute.For<ILogger<HttpCatalogClient>>()), handler);
    }

    private static CatalogProductInfo MakeInfo(Guid id) =>
        new(id, "Widget", 9.99m, 10, true);


    [Fact]
    public async Task GetProductById_ReturnsProduct_OnSuccess()
    {
        var id = Guid.NewGuid();
        var (client, handler) = Build(HttpStatusCode.OK, MakeInfo(id));

        var result = await client.GetProductByIdAsync(id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(id);
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
    public async Task GetProductInfos_EmptyList_SkipsHttpCall_ReturnsEmpty()
    {
        var (client, handler) = Build(HttpStatusCode.OK);

        var result = await client.GetProductInfosByIdsAsync([]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        handler.LastRequest.Should().BeNull();
    }

    [Fact]
    public async Task GetProductInfos_BuildsQueryString_UsesInfosEndpoint()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var body = ids.ToDictionary(id => id, MakeInfo);
        var (client, handler) = Build(HttpStatusCode.OK, body);

        var result = await client.GetProductInfosByIdsAsync(ids);

        result.IsSuccess.Should().BeTrue();
        var path = handler.LastRequest!.RequestUri!.PathAndQuery;
        path.Should().StartWith("/internal/products/infos?");
        path.Should().Contain($"ids={ids[0]}").And.Contain($"ids={ids[1]}");
    }

    [Fact]
    public async Task GetProductInfos_ReturnsEmpty_OnNonSuccess()
    {
        var ids = new[] { Guid.NewGuid() };
        var (client, _) = Build(HttpStatusCode.ServiceUnavailable);

        var result = await client.GetProductInfosByIdsAsync(ids);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
