using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using OrderSphere.Catalog.Api;
using OrderSphere.Catalog.Application.Abstractions;
using OrderSphere.Catalog.Infrastructure.Persistence;
using StackExchange.Redis;

namespace OrderSphere.IntegrationTests.Api;

/// <summary>
/// Boots the Catalog API in-process. The relational store is replaced with in-memory SQLite (the
/// Product aggregate projects an owned Money type), the Redis L2 cache is removed so HybridCache runs
/// L1-only, and the four hosted services (reservation sweeper, search/blob initializers, data seeder)
/// are stripped. Blob storage and Azure AI Search stay in their unconfigured (disabled) state.
/// </summary>
public sealed class CatalogApiFactory : WebApplicationFactory<ApiMarker>
{
    /// <summary>Stubs the cross-service purchase check; review creation treats every customer as a verified purchaser.</summary>
    public IOrderingClient OrderingClient { get; } = Substitute.For<IOrderingClient>();

    public CatalogApiFactory()
    {
        OrderingClient.HasPurchasedAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ApplyTestConfig();

        builder.ConfigureTestServices(services =>
        {
            services.AddTestAuthentication();
            services.RemoveHostedServices();
            services.UseSqliteDb<CatalogDbContext>();

            // Drop the eagerly-connected Redis registrations; HybridCache falls back to its L1 store.
            services.RemoveAll<IConnectionMultiplexer>();
            services.RemoveAll<IDistributedCache>();

            services.RemoveAll<IOrderingClient>();
            services.AddSingleton(OrderingClient);
        });
    }
}
