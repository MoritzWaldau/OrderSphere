using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;

namespace OrderSphere.Basket.Api.Configuration;

public static class RateLimitingExtensions
{
    public const string CartPolicy = "cart-api";

    // D3 — distributed rate-limiting: counters live in Redis so the quota is shared across
    // every Basket instance instead of one quota per process.
    public static IServiceCollection AddBasketRateLimiting(
        this IServiceCollection services, IConnectionMultiplexer multiplexer)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(CartPolicy, _ =>
                RedisRateLimitPartition.GetRedisFixedWindowLimiter(
                    CartPolicy, multiplexer, permitLimit: 100, window: TimeSpan.FromSeconds(60)));
        });

        return services;
    }
}
