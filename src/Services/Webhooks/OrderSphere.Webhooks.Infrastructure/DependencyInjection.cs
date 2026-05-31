using Microsoft.Extensions.DependencyInjection;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Inbox;
using OrderSphere.BuildingBlocks.EventBus.Inbox;
using OrderSphere.Webhooks.Infrastructure.Persistence;

namespace OrderSphere.Webhooks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWebhooksInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IInboxStore, EfInboxStore<WebhooksDbContext>>();
        return services;
    }
}
