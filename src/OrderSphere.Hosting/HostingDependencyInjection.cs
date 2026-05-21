using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderSphere.Application;
using OrderSphere.Infrastructure;

namespace OrderSphere.Hosting;

public static class HostingDependencyInjection
{
    public static IHostApplicationBuilder AddOrderSphereCore(this IHostApplicationBuilder builder)
    {
        builder.AddAzureServiceBusClient("azure-service-bus");

        builder.Services
            .AddApplicationServices(builder.Configuration)
            .AddInfrastructureServices(builder.Configuration, builder.Environment);

        return builder;
    }
}
