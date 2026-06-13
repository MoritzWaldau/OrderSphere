using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using OrderSphere.Bff.Auth;
using OrderSphere.BuildingBlocks.Security;

namespace OrderSphere.Bff.Extensions;

internal static class BffAuthenticationExtensions
{
    public static void AddBffAuthentication(
        this WebApplicationBuilder builder,
        string keycloakAuthority,
        string keycloakClientId,
        string keycloakClientSecret,
        bool isProduction)
    {
        // Shared cached JWKS — used by RefreshTokenHandler and BackchannelLogoutEndpoint.
        builder.Services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(
            new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{keycloakAuthority}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = isProduction }));

        builder.Services.AddSingleton<ITicketStore, RedisTicketStore>();
        builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<ITicketStore>((opts, store) => opts.SessionStore = store);

        builder.Services.AddTransient<RefreshTokenHandler>();
        builder.Services.AddSecurityAuditLogger();
        builder.Services.AddHttpClient("keycloak-token");

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = isProduction ? "__Host-ordersphere.bff" : "ordersphere.bff";
                options.Cookie.SameSite = isProduction ? SameSiteMode.Strict : SameSiteMode.Lax;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = isProduction
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
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
                    // For /api/* requests return 401 instead of a 302 redirect to Keycloak
                    // so the WASM client can handle unauthenticated state gracefully.
                    OnRedirectToIdentityProvider = ctx =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/api"))
                        {
                            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            ctx.HandleResponse();
                        }

                        return Task.CompletedTask;
                    },

                    // Keycloak puts realm roles in realm_access.roles of the ACCESS token.
                    // Copy them to the identity before the session is persisted.
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

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("BffUserPolicy", p => p.RequireAuthenticatedUser());
    }
}
