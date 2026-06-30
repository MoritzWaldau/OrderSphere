using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Dlq;
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

    /// <summary>
    /// Registers the dead-letter admin surface for a worker host: the <see cref="IDlqAdmin"/> over the
    /// given owned queues, the background depth monitor, and the <c>ordersphere.dlq.depth</c> gauge.
    /// Map the HTTP endpoints separately with <c>MapDlqAdminEndpoints</c>. Requires a registered
    /// <see cref="Azure.Messaging.ServiceBus.ServiceBusClient"/>.
    /// </summary>
    public static IServiceCollection AddDlqAdmin(this IServiceCollection services, params string[] ownedQueues)
    {
        var options = new DlqAdminOptions { OwnedQueues = ownedQueues };
        var cache = new DlqDepthCache();

        // Register the gauge once, bound to the shared cache the monitor populates.
        DlqMetrics.Register(cache);

        services.AddSingleton(options);
        services.AddSingleton(cache);
        services.AddSingleton<IDlqAdmin, ServiceBusDlqAdmin>();
        services.AddHostedService<DlqDepthMonitor>();
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
