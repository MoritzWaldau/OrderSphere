using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace OrderSphere.Catalog.Api.Configuration;

public static class RateLimitingExtensions
{
    public const string PublicPolicy = "public-api";
    public const string AdminPolicy = "admin-api";

    public static IServiceCollection AddCatalogRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter(PublicPolicy, o =>
            {
                o.Window = TimeSpan.FromSeconds(60);
                o.PermitLimit = 100;
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter(AdminPolicy, o =>
            {
                o.Window = TimeSpan.FromSeconds(60);
                o.PermitLimit = 30;
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit = 0;
            });
        });

        return services;
    }
}
