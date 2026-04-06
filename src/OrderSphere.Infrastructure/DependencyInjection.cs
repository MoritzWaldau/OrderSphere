using Azure.Communication.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.ServiceBus;
using OrderSphere.Domain.Configuration;
using OrderSphere.Infrastructure.Email;
using OrderSphere.Infrastructure.Interceptors;
using OrderSphere.Infrastructure.Persistence;
using OrderSphere.Infrastructure.ServiceBus;

namespace OrderSphere.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrderSphereDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("ordersphere-db"));
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            options.AddInterceptors(new AuditSaveChangesInterceptor());
        });

        services.AddScoped<IDbContext, OrderSphereDbContext>();
        services.AddScoped<IServiceBusPublisher, ServiceBusPublisher>();

        //Mail service
        services.AddScoped<IEmailService, EmailService>();
        services.Configure<MailConfiguration>(configuration.GetSection("MailServiceConfiguration"));
        return services;
    }
}
