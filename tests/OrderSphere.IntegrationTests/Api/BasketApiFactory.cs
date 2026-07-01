using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using OrderSphere.Basket.Api;
using OrderSphere.Basket.Api.Configuration;
using OrderSphere.Basket.Application.Abstractions;
using OrderSphere.Basket.Infrastructure.Persistence;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.IntegrationTests.Api;

/// <summary>
/// Boots the Basket API in-process with an in-memory database, the test auth scheme and a stubbed
/// <see cref="ICatalogClient"/> so cart operations do not reach out to the Catalog service. Each
/// product the stub is asked about is reported in stock.
/// </summary>
public sealed class BasketApiFactory : WebApplicationFactory<ApiMarker>
{
    public ICatalogClient CatalogClient { get; } = Substitute.For<ICatalogClient>();

    public BasketApiFactory()
    {
        CatalogClient.GetProductByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<CatalogProductInfo>.Success(
                new CatalogProductInfo(call.Arg<Guid>(), "Test Product", 19.99m, Stock: 100, IsActive: true)));

        CatalogClient.GetProductInfosByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                IReadOnlyDictionary<Guid, CatalogProductInfo> infos = call.Arg<IEnumerable<Guid>>()
                    .ToDictionary(id => id, id => new CatalogProductInfo(id, "Test Product", 19.99m, 100, true));
                return Result<IReadOnlyDictionary<Guid, CatalogProductInfo>>.Success(infos);
            });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ApplyTestConfig();

        builder.ConfigureTestServices(services =>
        {
            services.UseInMemoryDb<BasketDbContext>("basket-tests");
            services.AddTestAuthentication();
            services.RemoveHostedServices();

            // D3's rate limiter calls Redis directly (see ApiTestHostExtensions.DisableRateLimiting).
            services.DisableRateLimiting(RateLimitingExtensions.CartPolicy);

            services.RemoveAll<ICatalogClient>();
            services.AddSingleton(CatalogClient);
        });
    }
}
