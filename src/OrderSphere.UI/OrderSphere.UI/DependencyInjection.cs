using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace OrderSphere.UI;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, loggerConfig) =>
        {
            loggerConfig.ReadFrom.Configuration(context.Configuration);
        });

        return builder;
    }

    public static WebApplicationBuilder AddServiceBus(this WebApplicationBuilder builder)
    {
        AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);
        builder.AddAzureServiceBusClient("azure-service-bus");

        return builder;
    }

    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        //services.AddSingleton(_ =>
        //{
        //    string connectionString = configuration.GetConnectionString("azure-service-bus") ?? throw new Exception("Unable to read connectionString: azure-service-bus");
        //    return new ServiceBusClient(connectionString);
        //});
        //
        return services;
    }
}
