using Asp.Versioning;

namespace OrderSphere.Webhooks.Api.Configuration;

public static class ApiVersioningExtensions
{
    public static IServiceCollection AddWebhooksApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });

        return services;
    }
}
