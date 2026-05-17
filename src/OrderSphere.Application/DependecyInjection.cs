using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OrderSphere.Application;

public static class DependecyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = configuration["MediatR:LicenseKey"] ?? throw new Exception("Unable to read licenseKey");
            cfg.RegisterServicesFromAssembly(typeof(DependecyInjection).Assembly);
        });

        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new()
            {
                LocalCacheExpiration = TimeSpan.FromMinutes(5),
                Expiration = TimeSpan.FromMinutes(30),
            };
        });

        return services;
    }
}
