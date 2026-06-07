using System.Security.Claims;
using System.Threading.RateLimiting;

namespace OrderSphere.Advisory.Api.Configuration;

// Per-user rate limiting for the advisory chat. Unlike the catalog/basket global
// limiters, /chat is partitioned by the caller's identity because each request
// drives an LLM completion — the cost and abuse surface is per user, not global.
public static class RateLimitingExtensions
{
    public const string ChatPolicy = "advisor-chat";

    public static IServiceCollection AddAdvisorRateLimiting(this IServiceCollection services)
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

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 20,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }
}
