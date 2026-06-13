using System.Security.Claims;
using System.Threading.RateLimiting;

namespace OrderSphere.Mcp.Server.Configuration;

// Per-user rate limiting for the MCP endpoint. Partitioned by caller identity
// (sub claim, IP fallback) like the advisory chat policy. The limit must stay
// well above the chat limit (20/min): one chat turn produces several MCP
// requests (initialize, tools/list, and one per tool call), all attributed to
// the same end user because the internal agent forwards the user's token.
public static class RateLimitingExtensions
{
    public const string McpPolicy = "mcp";

    public static IServiceCollection AddMcpRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(McpPolicy, httpContext =>
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
                        PermitLimit = 120,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }
}
