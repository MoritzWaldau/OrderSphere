using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Inbox;
using OrderSphere.BuildingBlocks.EventBus.Inbox;
using OrderSphere.BuildingBlocks.EventBus.Outbox;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Infrastructure.Outbox;
using OrderSphere.Ordering.Infrastructure.Persistence;
using OrderSphere.Ordering.Infrastructure.ServiceBus;

namespace OrderSphere.Ordering.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingInfrastructure(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        services.AddScoped<IOrderingDbContext>(sp => sp.GetRequiredService<OrderingDbContext>());

        // Shipping rate (flat rate, waived above a free-shipping threshold).
        services.AddSingleton<IShippingRateProvider, Shipping.FlatRateShippingProvider>();

        // Outbox: writes to DB, dispatched by OutboxDispatcher background service.
        services.AddScoped<IOrderingServiceBusPublisher, OutboxPublisher>();
        services.AddSingleton<RealServiceBusPublisher>();
        services.AddScoped<IOutboxEventHandler, CheckoutCartEventHandler>();
        services.AddScoped<IOutboxEventHandler, PaymentRequestedEventHandler>();
        services.AddScoped<IOutboxEventHandler, OrderPlacedEventHandler>();
        services.AddScoped<IOutboxEventHandler, RealtimeNotificationEventHandler>();
        services.AddScoped<IOutboxEventHandler, OrderStatusChangedEventHandler>();

        // Inbox: idempotency for consumed integration events.
        services.AddScoped<IInboxStore, EfInboxStore<OrderingDbContext>>();

        return services;
    }

    /// <summary>
    /// Registers OutboxDispatcher and OutboxCleanupService as hosted background services.
    /// Call from both Ordering.Api and Ordering.Worker hosts.
    /// </summary>
    public static IServiceCollection AddOrderingOutboxProcessing(this IServiceCollection services)
        => services.AddOutboxProcessing<OrderingDbContext>();
}
