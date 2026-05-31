using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderSphere.Basket.Application.Abstractions;
using OrderSphere.Basket.Infrastructure.Persistence;

namespace OrderSphere.Basket.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddBasketInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<BasketDbContext>("basket-db", settings =>
        {
            settings.DisableRetry = false;
        });

        builder.Services.AddScoped<IBasketDbContext>(sp =>
            sp.GetRequiredService<BasketDbContext>());

        return builder;
    }
}
