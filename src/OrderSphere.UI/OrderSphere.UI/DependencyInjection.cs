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
}
