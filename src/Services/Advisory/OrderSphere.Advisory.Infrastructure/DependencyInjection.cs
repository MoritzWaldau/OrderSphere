using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderSphere.Advisory.Application.Abstractions;
using OrderSphere.Advisory.Infrastructure.Persistence;

namespace OrderSphere.Advisory.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddAdvisoryInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<AdvisoryDbContext>("advisory-db", settings =>
        {
            settings.DisableRetry = false;
        });

        builder.Services.AddScoped<IAdvisoryDbContext>(sp =>
            sp.GetRequiredService<AdvisoryDbContext>());

        return builder;
    }
}
