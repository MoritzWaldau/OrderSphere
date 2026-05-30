using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using OrderSphere.Bff.Auth;
using OrderSphere.Bff.Hubs;
using OrderSphere.Bff.Workers;
using OrderSphere.BuildingBlocks.Security;
using StackExchange.Redis;
using System.Security.Claims;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ── Configuration ────────────────────────────────────────────────────────────

var keycloakAuthority = builder.Configuration["Keycloak:Authority"]
    ?? throw new InvalidOperationException("Keycloak:Authority is not configured.");
var keycloakClientId = builder.Configuration["Keycloak:ClientId"] ?? "web-bff";
var keycloakClientSecret = builder.Configuration["Keycloak:ClientSecret"]
    ?? throw new InvalidOperationException("Keycloak:ClientSecret is not configured.");
// "Testing" is treated like Development: no HTTPS enforcement, no HSTS.
// Staging and Production environments are treated as production.
var isProduction = !builder.Environment.IsDevelopment()
                && !builder.Environment.IsEnvironment("Testing");

// ── Redis (session ticket store + DataProtection keys) ───────────────────────
// Connection string injected by Aspire via WithReference(redis).
// In dev without Aspire, set ConnectionStrings__redis in appsettings.Development.json.
builder.AddRedisDistributedCache("redis");

// Persist DataProtection keys to Redis so all BFF instances share the same key ring.
// Without persistence, keys are regenerated on every restart, invalidating all active
// sessions encrypted and stored in Redis by RedisTicketStore.
// A dedicated IConnectionMultiplexer is created from the Aspire-injected connection
// string; this avoids the keyed-service DI resolution issue with the Aspire Redis
// registration while keeping the DataProtection key store fully isolated.
// In the "Testing" environment the key ring is ephemeral (no Redis needed).
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDataProtection()
        .SetApplicationName("OrderSphere.Bff")
        .PersistKeysToStackExchangeRedis(
            ConnectionMultiplexer.Connect(
                builder.Configuration.GetConnectionString("redis")
                ?? throw new InvalidOperationException(
                    "Redis connection string not configured. " +
                    "Ensure the Aspire Redis resource is referenced by the BFF.")),
            "DataProtection-Keys");
}
else
{
    // Testing: ephemeral key ring — sessions are valid only within one test run.
    builder.Services.AddDataProtection()
        .SetApplicationName("OrderSphere.Bff");
}

// ── SignalR + Redis backplane ─────────────────────────────────────────────
builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("redis")!);

// ── Azure Service Bus (for realtime notification consumption) ────────────
builder.AddAzureServiceBusClient("azure-service-bus");
builder.Services.AddHostedService<RealtimeNotificationProcessor>();

// ── Antiforgery ───────────────────────────────────────────────────────────────
// Cookie is non-HttpOnly so the WASM client can read it via /bff/user response.
// The request token is included in the /bff/user JSON payload (xsrfToken field)
// and attached to mutating requests as X-XSRF-TOKEN by AntiforgeryDelegatingHandler.
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

// ── Security audit logger ─────────────────────────────────────────────────────
builder.Services.AddSecurityAuditLogger();

// ── HTTP client for Keycloak token endpoint (used by RefreshTokenHandler) ────
builder.Services.AddHttpClient("keycloak-token");

// ── OIDC configuration manager (singleton, cached JWKS) ──────────────────────
// Shared by RefreshTokenHandler and BackchannelLogoutEndpoint for logout_token validation.
builder.Services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(
    new ConfigurationManager<OpenIdConnectConfiguration>(
        $"{keycloakAuthority}/.well-known/openid-configuration",
        new OpenIdConnectConfigurationRetriever(),
        new HttpDocumentRetriever { RequireHttps = isProduction }));

// ── Session ticket store ──────────────────────────────────────────────────────
// Tickets are stored in Redis (serialised + DataProtection-encrypted).
// This enables multi-instance BFF deployments and is a prerequisite for
// individual session revocation via back-channel logout (Phase 4).
builder.Services.AddSingleton<ITicketStore, RedisTicketStore>();

// Wire the ticket store into CookieAuthenticationOptions after DI is fully configured.
builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<ITicketStore>((opts, store) => opts.SessionStore = store);

// ── Refresh-token rotation ────────────────────────────────────────────────────
// Registered as transient so DI injects it per-request into CookieAuthenticationEvents.
builder.Services.AddTransient<RefreshTokenHandler>();

// ── Authentication ────────────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        // Use __Host- prefix in production: requires Secure + Path=/ + no Domain.
        // In development (HTTP), the browser would reject __Host- cookies.
        options.Cookie.Name = isProduction ? "__Host-ordersphere.bff" : "ordersphere.bff";
        options.Cookie.SameSite = isProduction ? SameSiteMode.Strict : SameSiteMode.Lax;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = isProduction
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
        options.Cookie.Path = "/";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        // Delegate ValidatePrincipal to the refresh-token rotation handler.
        options.EventsType = typeof(RefreshTokenHandler);
    })
    .AddOpenIdConnect(options =>
    {
        options.Authority = keycloakAuthority;
        options.ClientId = keycloakClientId;
        options.ClientSecret = keycloakClientSecret;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.RequireHttpsMetadata = isProduction;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("roles");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "preferred_username",
            RoleClaimType = "roles",
        };
        options.MapInboundClaims = false;
        options.Events = new OpenIdConnectEvents
        {
            // Keycloak puts realm roles in realm_access.roles of the ACCESS token,
            // not the ID token. Read the raw access token here and add individual
            // "roles" claims to the identity before the session is persisted.
            OnTokenValidated = ctx =>
            {
                if (ctx.Principal?.Identity is not ClaimsIdentity identity)
                    return Task.CompletedTask;

                var accessToken = ctx.TokenEndpointResponse?.AccessToken;
                if (string.IsNullOrEmpty(accessToken))
                    return Task.CompletedTask;

                try
                {
                    var jwt = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(accessToken);

                    if (jwt.TryGetClaim("realm_access", out var realmAccessClaim))
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(realmAccessClaim.Value);
                        if (doc.RootElement.TryGetProperty("roles", out var rolesEl))
                        {
                            foreach (var role in rolesEl.EnumerateArray())
                            {
                                var value = role.GetString();
                                if (!string.IsNullOrEmpty(value) && !identity.HasClaim("roles", value))
                                    identity.AddClaim(new Claim("roles", value));
                            }
                        }
                    }
                }
                catch { /* malformed access token — skip */ }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                var auditLogger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ISecurityAuditLogger>();
                auditLogger.Log(new SecurityAuditEvent(
                    SecurityAuditEventType.LoginFailure,
                    IpAddress: ctx.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Details: ctx.Exception.GetType().Name));
                return Task.CompletedTask;
            }
        };
    });

// ── Authorization ─────────────────────────────────────────────────────────────
// BffUserPolicy: every request forwarded to the API gateway requires an
// authenticated session. Future public passthrough routes can use AllowAnonymous.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("BffUserPolicy", p => p.RequireAuthenticatedUser());

// ── Reverse proxy ─────────────────────────────────────────────────────────────
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver()
    .AddTransforms(transforms =>
    {
        transforms.AddRequestTransform(async ctx =>
        {
            var token = await ctx.HttpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                ctx.ProxyRequest.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        });
    });

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
        // CSP for Blazor WASM: wasm-unsafe-eval is required for the .wasm module.
        // Adjust frame-src / connect-src if Keycloak runs on a separate host in production.
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

// Logout: sign out both cookie and OIDC schemes.
// The OIDC handler reads the saved id_token (from the Redis ticket) and includes
// it as id_token_hint in the end_session_endpoint redirect, terminating the
// Keycloak SSO session.
app.MapPost("/bff/logout", (HttpContext _) =>
    Results.SignOut(
        new AuthenticationProperties { RedirectUri = "/" },
        [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
    .AddEndpointFilter<AntiforgeryEndpointFilter>();

// Returns user info from the server-side session.
// Also issues/refreshes the antiforgery token pair on every call so the
// WASM client always has a current XSRF request token.
// Unauthenticated callers receive isAuthenticated:false rather than a redirect.
app.MapGet("/bff/user", (HttpContext ctx, IAntiforgery antiforgery) =>
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
            xsrfToken = tokens.RequestToken,
        });

    return Results.Ok(new
    {
        isAuthenticated = true,
        sub   = user.FindFirst("sub")?.Value
             ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        name  = user.Identity.Name,
        email = user.FindFirst("email")?.Value
             ?? user.FindFirst(ClaimTypes.Email)?.Value,
        roles = user.FindAll("roles").Select(c => c.Value).ToArray(),
        xsrfToken = tokens.RequestToken,
    });
});

// ── DEBUG: dump all claims — REMOVE before going to production ───────────────
if (app.Environment.IsDevelopment())
{
    app.MapGet("/bff/debug/claims", (HttpContext ctx) =>
        Results.Ok(ctx.User.Claims.Select(c => new { c.Type, c.Value }).ToArray()))
       .RequireAuthorization();
}

// Back-channel logout stub — Phase 4 will add JWT validation + session revocation.
BackchannelLogoutEndpoint.Map(app);

// ── Antiforgery middleware for /api/* mutations ───────────────────────────────
// Validates X-XSRF-TOKEN before forwarding any non-GET /api request to the gateway.
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

// Default policy for all proxied routes. Routes with AuthorizationPolicy: "anonymous"
// in config (catalog-public-products, catalog-public-categories) override this via
// [AllowAnonymous], so public catalog GETs pass through without a session.
app.MapReverseProxy().RequireAuthorization("BffUserPolicy");
app.MapFallbackToFile("index.html");

app.Run();

// Expose Program for WebApplicationFactory<Program> in integration tests.
public partial class Program { }
