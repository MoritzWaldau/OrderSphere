using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddOrderSphereJwtAuth();

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Named limiters available for endpoint-level policies via [EnableRateLimiting].
    options.AddFixedWindowLimiter("gateway-global", cfg =>
    {
        cfg.PermitLimit = 200;
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 10;
    });

    options.AddFixedWindowLimiter("gateway-authenticated", cfg =>
    {
        cfg.PermitLimit = 100;
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 5;
    });

    // Global limiter runs after UseAuthentication() so the sub claim is available.
    // Authenticated users are partitioned per user-id (120 req/min) to prevent a
    // compromised token from consuming the IP quota of other users on shared egress
    // (NAT, corporate proxies). Anonymous callers fall back to per-IP (30 req/min).
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var sub = context.User.FindFirst("sub")?.Value;
        if (sub is not null)
        {
            return RateLimitPartition.GetFixedWindowLimiter($"user:{sub}", _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1)
                });
        }

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter($"ip:{clientIp}", _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1)
            });
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

app.MapReverseProxy();
app.MapHealthChecks("/health/gateway");

app.Run();
