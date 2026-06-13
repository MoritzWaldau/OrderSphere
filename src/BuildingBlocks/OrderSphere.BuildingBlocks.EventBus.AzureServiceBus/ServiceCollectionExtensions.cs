using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Outbox;

namespace OrderSphere.BuildingBlocks.EventBus.AzureServiceBus;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureServiceBusEventBus(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, AzureServiceBusEventBus>();
        return services;
    }

    public static IServiceCollection AddOutboxProcessing<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddHostedService<OutboxDispatcher<TContext>>();
        services.AddHostedService<OutboxCleanupService<TContext>>();
        return services;
    }
}
