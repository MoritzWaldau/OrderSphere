using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderSphere.Application;
using OrderSphere.Domain.Entities;
using OrderSphere.Infrastructure;
using OrderSphere.Infrastructure.Persistence;

namespace OrderSphere.Hosting;

public static class HostingDependencyInjection
{
    public static IHostApplicationBuilder AddOrderSphereCore(this IHostApplicationBuilder builder)
    {
        builder.AddAzureServiceBusClient("azure-service-bus");

        builder.Services
            .AddApplicationServices(builder.Configuration)
            .AddInfrastructureServices(builder.Configuration);

        builder.Services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<OrderSphereDbContext>();

        return builder;
    }
}
