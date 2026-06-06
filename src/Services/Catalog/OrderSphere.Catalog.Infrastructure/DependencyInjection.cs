using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderSphere.Catalog.Infrastructure.Persistence;

namespace OrderSphere.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddCatalogInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<CatalogDbContext>("catalog-db", settings =>
        {
            settings.DisableRetry = false;
        });

        builder.Services.AddScoped<ICatalogDbContext>(sp =>
            sp.GetRequiredService<CatalogDbContext>());

        // The Redis L2 (IDistributedCache) is registered by the API host via
        // AddOrderSphereRedisAsync, which adds Entra ID authentication for Azure Managed Redis.
        builder.Services.AddHybridCache();

        return builder;
    }
}
