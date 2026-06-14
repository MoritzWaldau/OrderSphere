using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using OrderSphere.ApiGateway.Onboarding;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddOrderSphereJwtAuth();

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

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

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 200,
            Window = TimeSpan.FromMinutes(1)
        });
    });
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IUserProfileStatusClient, UserProfileStatusClient>(c =>
    c.BaseAddress = new Uri("http://ordersphere-userprofile"))
    .AddServiceDiscovery();

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

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<OnboardingGateMiddleware>();

app.MapReverseProxy();
app.MapHealthChecks("/health/gateway");

app.Run();
