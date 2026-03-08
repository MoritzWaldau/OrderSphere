using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderSphere.Infrastructure.Interceptors;
using OrderSphere.Infrastructure.Persistence;

namespace OrderSphere.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrderSphereDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("postgres"));
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            options.AddInterceptors(new AuditSaveChangesInterceptor());
        });
            
        return services;
    }
}
