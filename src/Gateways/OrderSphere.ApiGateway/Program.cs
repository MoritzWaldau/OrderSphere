using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddOrderSphereJwtAuth();

builder.Services.AddAuthorization();

// D3 — distributed rate-limiting: gateway limiters share their quota counters across
// every gateway instance via Redis instead of counting in-process.
var redisMultiplexer = await builder.AddOrderSphereRedisAsync();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Named limiters available for endpoint-level policies via [EnableRateLimiting].
    options.AddPolicy("gateway-global", _ =>
        RedisRateLimitPartition.GetRedisFixedWindowLimiter(
            "gateway-global", redisMultiplexer, permitLimit: 200, window: TimeSpan.FromMinutes(1)));

    options.AddPolicy("gateway-authenticated", _ =>
        RedisRateLimitPartition.GetRedisFixedWindowLimiter(
            "gateway-authenticated", redisMultiplexer, permitLimit: 100, window: TimeSpan.FromMinutes(1)));

    // Global limiter runs after UseAuthentication() so the sub claim is available.
    // Authenticated users are partitioned per user-id (120 req/min) to prevent a
    // compromised token from consuming the IP quota of other users on shared egress
    // (NAT, corporate proxies). Anonymous callers fall back to per-IP (30 req/min).
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var sub = context.User.FindFirst("sub")?.Value;
        if (sub is not null)
        {
            return RedisRateLimitPartition.GetRedisFixedWindowLimiter(
                $"user:{sub}", redisMultiplexer, permitLimit: 120, window: TimeSpan.FromMinutes(1));
        }

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RedisRateLimitPartition.GetRedisFixedWindowLimiter(
            $"ip:{clientIp}", redisMultiplexer, permitLimit: 30, window: TimeSpan.FromMinutes(1));
    });
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapDefaultEndpoints();

app.Use(async (context, next) =>
{
    if (!context.Request.Headers.ContainsKey("X-Request-Id"))
    {
        context.Request.Headers["X-Request-Id"] = Guid.NewGuid().ToString("N");
    }
    context.Response.Headers["X-Request-Id"] = context.Request.Headers["X-Request-Id"].ToString();
    await next();
});

// Authentication before rate limiting so the GlobalLimiter can partition on the sub claim.
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseOrderSphereRequestLogging();

app.MapReverseProxy();
app.MapHealthChecks("/health/gateway");

app.Run();
