using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.ServiceBus;
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
            options.UseNpgsql(configuration.GetConnectionString("postgres"));
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            options.AddInterceptors(new AuditSaveChangesInterceptor());
        });

        services.AddScoped<IDbContext, OrderSphereDbContext>();

        services.AddScoped<IServiceBusPublisher, ServiceBusPublisher>();
        return services;
    }
}
