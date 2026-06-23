using Grpc.Core;
using OrderSphere.Basket.Infrastructure.CatalogClient;
using OrderSphere.Catalog.V1;

namespace OrderSphere.Basket.Tests.Infrastructure;

/// <summary>
/// Mapping and degradation behaviour of the gRPC-backed catalog client: gRPC responses are
/// translated to <see cref="OrderSphere.BuildingBlocks.Primitives.Result{T}"/> values, and an
/// <see cref="RpcException"/> (transport failure) is caught — single lookups fail, bulk lookups
/// degrade to an empty map.
/// </summary>
public sealed class GrpcCatalogClientTests
{
    private static GrpcCatalogClient Build(CatalogService.CatalogServiceClient client) =>
        new(client, Substitute.For<ILogger<GrpcCatalogClient>>());

    private static AsyncUnaryCall<T> Ok<T>(T value) => new(
        Task.FromResult(value),
        Task.FromResult(new Metadata()),
        () => Status.DefaultSuccess,
        () => new Metadata(),
        () => { });

    private static AsyncUnaryCall<T> Faulted<T>() => new(
        Task.FromException<T>(new RpcException(new Status(StatusCode.Unavailable, "catalog down"))),
        Task.FromResult(new Metadata()),
        () => new Status(StatusCode.Unavailable, "catalog down"),
        () => new Metadata(),
        () => { });

    [Fact]
    public async Task GetProductById_ReturnsProduct_OnFound()
    {
        var id = Guid.NewGuid();
        var client = Substitute.For<CatalogService.CatalogServiceClient>();
        client.GetProductAsync(Arg.Any<GetProductRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(Ok(new GetProductResponse
            {
                Found = true,
                Id = id.ToString(),
                Name = "Widget",
                Price = 9.99,
                Stock = 10,
                IsActive = true,
            }));

        var result = await Build(client).GetProductByIdAsync(id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(id);
        result.Value.Price.Should().Be(9.99m);
        result.Value.Stock.Should().Be(10);
    }

    [Fact]
    public async Task GetProductById_ReturnsFailure_WhenNotFound()
    {
        var client = Substitute.For<CatalogService.CatalogServiceClient>();
        client.GetProductAsync(Arg.Any<GetProductRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(Ok(new GetProductResponse { Found = false }));

        var result = await Build(client).GetProductByIdAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Catalog.ProductNotFound");
    }

    [Fact]
    public async Task GetProductById_TransportError_ReturnsUnavailable()
    {
        var client = Substitute.For<CatalogService.CatalogServiceClient>();
        client.GetProductAsync(Arg.Any<GetProductRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(Faulted<GetProductResponse>());

        var result = await Build(client).GetProductByIdAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Catalog.Unavailable");
    }

    [Fact]
    public async Task GetProductInfos_EmptyList_SkipsCall_ReturnsEmpty()
    {
        var client = Substitute.For<CatalogService.CatalogServiceClient>();

        var result = await Build(client).GetProductInfosByIdsAsync([]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        _ = client.DidNotReceiveWithAnyArgs().GetProductsAsync(default!);
    }

    [Fact]
    public async Task GetProductInfos_MapsFoundProducts_ById()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var response = new GetProductsResponse();
        response.Products.Add(new GetProductResponse { Found = true, Id = ids[0].ToString(), Name = "A", Price = 1.5, Stock = 3, IsActive = true });
        response.Products.Add(new GetProductResponse { Found = true, Id = ids[1].ToString(), Name = "B", Price = 2.5, Stock = 4, IsActive = true });

        var client = Substitute.For<CatalogService.CatalogServiceClient>();
        client.GetProductsAsync(Arg.Any<GetProductsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(Ok(response));

        var result = await Build(client).GetProductInfosByIdsAsync(ids);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[ids[0]].Name.Should().Be("A");
        result.Value[ids[1]].Price.Should().Be(2.5m);
    }

    [Fact]
    public async Task GetProductInfos_TransportError_DegradesToEmpty()
    {
        var client = Substitute.For<CatalogService.CatalogServiceClient>();
        client.GetProductsAsync(Arg.Any<GetProductsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(Faulted<GetProductsResponse>());

        var result = await Build(client).GetProductInfosByIdsAsync([Guid.NewGuid()]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
