using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.ServiceBus;
using OrderSphere.Domain.Configuration;
using OrderSphere.Infrastructure.CatalogClient;
using OrderSphere.Infrastructure.Email;
using OrderSphere.Infrastructure.Interceptors;
using OrderSphere.Infrastructure.OrderingClient;
using OrderSphere.Infrastructure.Outbox;
using OrderSphere.Infrastructure.Persistence;
using OrderSphere.Infrastructure.ServiceBus;

namespace OrderSphere.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddDbContext<OrderSphereDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("ordersphere-db"));
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            options.AddInterceptors(new AuditSaveChangesInterceptor());

            if (environment.IsDevelopment())
                options.EnableSensitiveDataLogging();
        });

        // Forward IDbContext to the same scoped instance that EF registers for
        // OrderSphereDbContext, so OutboxPublisher and command handlers share the
        // same context — and therefore the same open transaction.
        services.AddScoped<IDbContext>(sp => sp.GetRequiredService<OrderSphereDbContext>());

        // Typed HTTP client for Catalog service
        services.AddHttpClient<ICatalogClient, HttpCatalogClient>(client =>
        {
            var catalogUrl = configuration["Services:Catalog:BaseUrl"]
                ?? "http://ordersphere-catalog";
            client.BaseAddress = new Uri(catalogUrl);
        });

        // Typed HTTP client for Ordering service (proxy for all cart/order/checkout/coupon operations)
        services.AddHttpClient<IOrderingClient, HttpOrderingClient>(client =>
        {
            var orderingUrl = configuration["Services:Ordering:BaseUrl"]
                ?? "http://ordersphere-ordering";
            client.BaseAddress = new Uri(orderingUrl);
        });

        // OutboxPublisher writes events to the outbox table; ServiceBusPublisher dispatches them to the real queue.
        services.AddScoped<IServiceBusPublisher, OutboxPublisher>();
        services.AddSingleton<ServiceBusPublisher>();

        services.AddScoped<IEmailService, EmailService>();
        services.Configure<MailConfiguration>(configuration.GetSection("MailServiceConfiguration"));
        return services;
    }

    public static IServiceCollection AddOutboxProcessing(this IServiceCollection services)
    {
        services.AddHostedService<OutboxDispatcher>();
        return services;
    }
}
