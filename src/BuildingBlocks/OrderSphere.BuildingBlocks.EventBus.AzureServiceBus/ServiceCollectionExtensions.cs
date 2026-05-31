using Microsoft.Extensions.DependencyInjection;

namespace OrderSphere.BuildingBlocks.EventBus.AzureServiceBus;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureServiceBusEventBus(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, AzureServiceBusEventBus>();
        return services;
    }
}
