using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;

namespace OrderSphere.Catalog.Api.Configuration;

public static class RateLimitingExtensions
{
    public const string PublicPolicy = "public-api";
    public const string AdminPolicy = "admin-api";

    // D3 — distributed rate-limiting: counters live in Redis so the quota is shared across
    // every Catalog instance instead of one quota per process.
    public static IServiceCollection AddCatalogRateLimiting(
        this IServiceCollection services, IConnectionMultiplexer multiplexer)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(PublicPolicy, _ =>
                RedisRateLimitPartition.GetRedisFixedWindowLimiter(
                    PublicPolicy, multiplexer, permitLimit: 100, window: TimeSpan.FromSeconds(60)));

            options.AddPolicy(AdminPolicy, _ =>
                RedisRateLimitPartition.GetRedisFixedWindowLimiter(
                    AdminPolicy, multiplexer, permitLimit: 30, window: TimeSpan.FromSeconds(60)));
        });

        return services;
    }
}
