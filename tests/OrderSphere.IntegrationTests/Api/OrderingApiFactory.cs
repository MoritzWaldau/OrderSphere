using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderSphere.Ordering.Api;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Infrastructure.Persistence;

namespace OrderSphere.IntegrationTests.Api;

/// <summary>
/// Boots the Ordering API in-process. The relational store, the Redis idempotency store and the
/// Service-Bus-bound hosted services are replaced with in-memory/no-op equivalents so the order and
/// coupon endpoints can be exercised without external infrastructure. The Catalog/Basket HTTP clients
/// are left registered but untouched — the endpoints under test never invoke them.
/// </summary>
public sealed class OrderingApiFactory : WebApplicationFactory<ApiMarker>
{
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
        });
    }
}
