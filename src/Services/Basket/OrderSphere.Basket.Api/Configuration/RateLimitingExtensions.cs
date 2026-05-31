using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace OrderSphere.Basket.Api.Configuration;

public static class RateLimitingExtensions
{
    public const string CartPolicy = "cart-api";

    public static IServiceCollection AddBasketRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter(CartPolicy, o =>
            {
                o.Window = TimeSpan.FromSeconds(60);
                o.PermitLimit = 100;
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit = 0;
            });
        });

        return services;
    }
}
