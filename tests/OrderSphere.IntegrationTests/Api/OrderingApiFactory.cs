using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Api;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Infrastructure.Persistence;

namespace OrderSphere.IntegrationTests.Api;

/// <summary>
/// Boots the Ordering API in-process. The relational store, the Redis idempotency store and the
/// Service-Bus-bound hosted services are replaced with in-memory/no-op equivalents. The Catalog and
/// Basket HTTP clients are substituted with stubs configured for a successful single-line checkout,
/// so the checkout endpoint can run without the downstream services.
/// </summary>
public sealed class OrderingApiFactory : WebApplicationFactory<ApiMarker>
{
    public static readonly Guid CheckoutProductId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    public ICatalogClient CatalogClient { get; } = Substitute.For<ICatalogClient>();
    public IBasketClient BasketClient { get; } = Substitute.For<IBasketClient>();
    public IOrderingServiceBusPublisher Publisher { get; } = Substitute.For<IOrderingServiceBusPublisher>();

    public OrderingApiFactory()
    {
        BasketClient.GetCartAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<BasketCartInfo>.Success(
                new BasketCartInfo(call.Arg<Guid>(), [new BasketCartItemInfo(CheckoutProductId, 1)])));
        BasketClient.ClearCartItemsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        CatalogClient.GetProductByIdAsync(CheckoutProductId, Arg.Any<CancellationToken>())
            .Returns(Result<CatalogProductInfo>.Success(
                new CatalogProductInfo(CheckoutProductId, "Checkout Product", 19.99m, 100, true)));
        CatalogClient.ReserveStockAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyList<ReservationItem>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        CatalogClient.ReleaseReservationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ApplyTestConfig();

        builder.ConfigureTestServices(services =>
        {
            services.AddTestAuthentication();
            // Strip the outbox/Service-Bus hosted services first; UseSqliteDb then adds the schema
            // initializer, which must survive as the sole background service.
            services.RemoveHostedServices();
            services.UseSqliteDb<OrderingDbContext>(); // relational: Order projects the owned Money type

            services.RemoveAll<ICheckoutIdempotencyStore>();
            services.AddScoped<ICheckoutIdempotencyStore, InMemoryCheckoutIdempotencyStore>();

            services.RemoveAll<ICatalogClient>();
            services.AddSingleton(CatalogClient);
            services.RemoveAll<IBasketClient>();
            services.AddSingleton(BasketClient);
            services.RemoveAll<IOrderingServiceBusPublisher>();
            services.AddSingleton(Publisher);
        });
    }
}
