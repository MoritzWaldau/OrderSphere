using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Outbox;
using OrderSphere.BuildingBlocks.Locking;

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
        // Fallback: hosts that do not register a Redis connection still resolve IDistributedLock.
        // The null implementation always grants the lease (safe for single-instance deployments).
        services.TryAddSingleton<IDistributedLock>(NullDistributedLock.Instance);
        services.AddHostedService<OutboxDispatcher<TContext>>();
        services.AddHostedService<OutboxCleanupService<TContext>>();
        return services;
    }
}
