using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MudBlazor.Services;
using Serilog;
using System.Globalization;

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
        services.ConfigureMudBlazor();
        return services;
    }

    private static void ConfigureMudBlazor(this IServiceCollection services)
    {
        services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;

            config.SnackbarConfiguration.PreventDuplicates = false;
            config.SnackbarConfiguration.NewestOnTop = false;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 10000;
            config.SnackbarConfiguration.HideTransitionDuration = 500;
            config.SnackbarConfiguration.ShowTransitionDuration = 500;
            config.SnackbarConfiguration.SnackbarVariant = Variant.Outlined;
        });
        services.Configure<PopoverOptions>(options =>
        {
            options.ThrowOnDuplicateProvider = false;
        });

        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
    }
}
