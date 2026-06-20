using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using OrderSphere.Bff.Auth;
using OrderSphere.Bff.Extensions;
using OrderSphere.Bff.Hubs;
using OrderSphere.Bff.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ── Configuration ────────────────────────────────────────────────────────────
var oidcAuthority = builder.Configuration["Oidc:Authority"]
    ?? throw new InvalidOperationException("Oidc:Authority is not configured.");
var oidcClientId = builder.Configuration["Oidc:ClientId"] ?? "web-bff";
var oidcClientSecret = builder.Configuration["Oidc:ClientSecret"]
    ?? throw new InvalidOperationException("Oidc:ClientSecret is not configured.");
// "Testing" is treated like Development: no HTTPS enforcement, no HSTS.
var isProduction = !builder.Environment.IsDevelopment()
                && !builder.Environment.IsEnvironment("Testing");

// ── Session (Redis DataProtection + SignalR backplane) ───────────────────────
await builder.AddBffSessionAsync();

// ── Azure Service Bus (realtime notifications) ───────────────────────────────
builder.AddAzureServiceBusClient("azure-service-bus");
builder.Services.AddHostedService<RealtimeNotificationProcessor>();

// ── Antiforgery ───────────────────────────────────────────────────────────────
builder.Services.AddAntiforgery(opts =>
{
    opts.HeaderName = "X-XSRF-TOKEN";
    opts.Cookie.Name = "XSRF-TOKEN";
    opts.Cookie.HttpOnly = false;
    opts.Cookie.SameSite = SameSiteMode.Strict;
    opts.Cookie.SecurePolicy = isProduction
        ? CookieSecurePolicy.Always
        : CookieSecurePolicy.SameAsRequest;
});

// ── Authentication & Authorization ───────────────────────────────────────────
builder.AddBffAuthentication(oidcAuthority, oidcClientId, oidcClientSecret, isProduction);

// ── UserProfile status client (onboarding check for /bff/user) ───────────────
builder.Services.AddHttpClient("userprofile-status", c =>
    c.BaseAddress = new Uri("https://ordersphere-userprofile"))
    .AddServiceDiscovery();

// ── Reverse proxy ─────────────────────────────────────────────────────────────
builder.AddBffProxy();

// ─────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

app.MapDefaultEndpoints();

// ── Security headers ──────────────────────────────────────────────────────────
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";

    if (!app.Environment.IsDevelopment())
    {
        ctx.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        ctx.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'wasm-unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "font-src 'self' data:; " +
            "img-src 'self' data: blob:; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'";
    }

    await next();
});

// ── Static files (Blazor WASM) ───────────────────────────────────────────────
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// ── BFF endpoints ─────────────────────────────────────────────────────────────
app.MapGet("/bff/login", (HttpContext ctx, string? returnUrl) =>
{
    var redirect = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
    return Results.Challenge(
        new AuthenticationProperties { RedirectUri = redirect },
        [OpenIdConnectDefaults.AuthenticationScheme]);
});

app.MapPost("/bff/logout", (HttpContext _) =>
    Results.SignOut(
        new AuthenticationProperties { RedirectUri = "/" },
        [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
    .AddEndpointFilter<AntiforgeryEndpointFilter>();

// Issues/refreshes the antiforgery token pair on every call.
// Unauthenticated callers receive isAuthenticated:false rather than a redirect.
app.MapGet("/bff/user", async (HttpContext ctx, IAntiforgery antiforgery) =>
{
    var tokens = antiforgery.GetAndStoreTokens(ctx);
    var user = ctx.User;

    if (user.Identity?.IsAuthenticated != true)
        return Results.Ok(new
        {
            isAuthenticated = false,
            sub = (string?)null,
            name = (string?)null,
            email = (string?)null,
            roles = Array.Empty<string>(),
            onboardingComplete = false,
            xsrfToken = tokens.RequestToken,
        });

    var sub = user.FindFirst("sub")?.Value
           ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    var onboardingComplete = false;
    try
    {
        var accessToken = await ctx.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(accessToken))
        {
            var factory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
            using var client = factory.CreateClient("userprofile-status");
            using var req = new HttpRequestMessage(
                HttpMethod.Get, "/api/v1/profile/onboarding-status");
            req.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var resp = await client.SendAsync(req, ctx.RequestAborted);
            if (resp.IsSuccessStatusCode)
                onboardingComplete = await resp.Content
                    .ReadFromJsonAsync<bool>(ctx.RequestAborted);
        }
    }
    catch
    {
        // UserProfile unreachable or no profile yet — treat as incomplete.
    }

    return Results.Ok(new
    {
        isAuthenticated = true,
        sub,
        name = user.Identity.Name,
        email = user.FindFirst("email")?.Value
             ?? user.FindFirst(ClaimTypes.Email)?.Value,
        roles = user.FindAll("roles").Select(c => c.Value).ToArray(),
        onboardingComplete,
        xsrfToken = tokens.RequestToken,
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapGet("/bff/debug/claims", (HttpContext ctx) =>
        Results.Ok(ctx.User.Claims.Select(c => new { c.Type, c.Value }).ToArray()))
       .RequireAuthorization();
}

BackchannelLogoutEndpoint.Map(app);

// ── Antiforgery middleware for /api/* mutations ───────────────────────────────
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api") &&
        !new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "GET", "HEAD", "OPTIONS", "TRACE" }
            .Contains(ctx.Request.Method))
    {
        var af = ctx.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            await af.ValidateRequestAsync(ctx);
        }
        catch (AntiforgeryValidationException)
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            await ctx.Response.WriteAsJsonAsync(new
            {
                error = "CSRF validation failed.",
                hint = "Include a valid X-XSRF-TOKEN header on all mutating requests."
            });
            return;
        }
    }

    await next();
});

// ── SignalR hub ───────────────────────────────────────────────────────────
app.MapHub<NotificationHub>("/hubs/notifications");

app.MapReverseProxy().RequireAuthorization("BffUserPolicy");
app.MapFallbackToFile("index.html");

app.Run();

// Expose Program for WebApplicationFactory<Program> in integration tests.
public partial class Program { }
