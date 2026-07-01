using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;

namespace OrderSphere.Advisory.Api.Configuration;

// Per-user rate limiting for the advisory chat. Unlike the catalog/basket global
// limiters, /chat is partitioned by the caller's identity because each request
// drives an LLM completion — the cost and abuse surface is per user, not global.
//
// D3 — distributed rate-limiting: counters live in Redis so a user's quota is shared
// across every Advisory instance instead of one quota per process.
public static class RateLimitingExtensions
{
    public const string ChatPolicy = "advisor-chat";

    public static IServiceCollection AddAdvisorRateLimiting(
        this IServiceCollection services, IConnectionMultiplexer multiplexer)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(ChatPolicy, httpContext =>
            {
                var partitionKey =
                    httpContext.User.FindFirst("sub")?.Value
                    ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";

                return RedisRateLimitPartition.GetRedisFixedWindowLimiter(
                    $"{ChatPolicy}:{partitionKey}", multiplexer, permitLimit: 20, window: TimeSpan.FromMinutes(1));
            });
        });

        return services;
    }
}
