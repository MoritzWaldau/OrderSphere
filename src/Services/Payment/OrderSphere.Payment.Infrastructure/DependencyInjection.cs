using Microsoft.Extensions.DependencyInjection;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Inbox;
using OrderSphere.BuildingBlocks.EventBus.Inbox;
using OrderSphere.Payment.Infrastructure.Outbox;
using OrderSphere.Payment.Infrastructure.Persistence;
using OrderSphere.Payment.Infrastructure.Providers;

namespace OrderSphere.Payment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IInboxStore, EfInboxStore<PaymentDbContext>>();

        // Outbox
        services.AddScoped<IOutboxEventHandler, PaymentProcessedEventHandler>();
        services.AddHostedService<OutboxDispatcher>();
        services.AddHostedService<OutboxCleanupService>();

        services.AddSingleton<IPaymentProvider, InvoicePaymentProvider>();
        services.AddSingleton<IPaymentProvider, CreditCardPaymentProvider>();
        services.AddSingleton<IPaymentProvider, PayPalPaymentProvider>();
        services.AddSingleton<IPaymentProviderFactory, PaymentProviderFactory>();

        return services;
    }
}
