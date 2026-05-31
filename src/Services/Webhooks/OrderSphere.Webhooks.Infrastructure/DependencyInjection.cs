using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Inbox;
using OrderSphere.BuildingBlocks.EventBus.Inbox;
using OrderSphere.Webhooks.Application.Abstractions;
using OrderSphere.Webhooks.Infrastructure.Persistence;

namespace OrderSphere.Webhooks.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddWebhooksInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<WebhooksDbContext>("webhooks-db", settings =>
        {
            settings.DisableRetry = false;
        });

        builder.Services.AddScoped<IWebhooksDbContext>(sp =>
            sp.GetRequiredService<WebhooksDbContext>());

        builder.Services.AddScoped<IInboxStore, EfInboxStore<WebhooksDbContext>>();

        return builder;
    }
}
