using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderSphere.Ordering.Infrastructure.Email;
using OrderSphere.Ordering.Infrastructure.Interceptors;
using OrderSphere.Ordering.Infrastructure.Outbox;
using OrderSphere.Ordering.Infrastructure.Persistence;
using OrderSphere.Ordering.Infrastructure.ServiceBus;

namespace OrderSphere.Ordering.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Ordering infrastructure services.
    /// Caller must also call host.AddNpgsqlDbContext&lt;OrderingDbContext&gt;("ordering-db") via Aspire.
    /// </summary>
    public static IServiceCollection AddOrderingInfrastructure(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        // Forward IOrderingDbContext to the same scoped instance registered by Aspire/EF.
        services.AddScoped<IOrderingDbContext>(sp => sp.GetRequiredService<OrderingDbContext>());

        // Outbox: writes to DB, dispatched by OutboxDispatcher background service.
        services.AddScoped<IOrderingServiceBusPublisher, OutboxPublisher>();
        services.AddSingleton<RealServiceBusPublisher>();

        // Email service for order confirmations.
        services.AddScoped<IOrderingEmailService, OrderingEmailService>();

        return services;
    }

    /// <summary>
    /// Registers OutboxDispatcher as a hosted background service.
    /// Call from both Ordering.Api and Ordering.Worker hosts.
    /// </summary>
    public static IServiceCollection AddOrderingOutboxProcessing(this IServiceCollection services)
    {
        services.AddHostedService<OutboxDispatcher>();
        return services;
    }
}
