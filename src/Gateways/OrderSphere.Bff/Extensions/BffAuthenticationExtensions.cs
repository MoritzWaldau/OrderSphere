using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
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
        string oidcAuthority,
        string oidcClientId,
        string oidcClientSecret,
        bool isProduction)
    {
        var oidcAudience = builder.Configuration["Oidc:Audience"]
            ?? "https://api.ordersphere.dev";
        var rolesClaim = builder.Configuration["Oidc:RolesClaim"]
            ?? "https://ordersphere.dev/roles";

        // Shared cached JWKS — used by RefreshTokenHandler and BackchannelLogoutEndpoint.
        builder.Services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(
            new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{oidcAuthority.TrimEnd('/')}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = isProduction }));

        builder.Services.AddSingleton<ITicketStore, RedisTicketStore>();
        builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<ITicketStore>((opts, store) => opts.SessionStore = store);

        builder.Services.AddTransient<RefreshTokenHandler>();
        builder.Services.AddSecurityAuditLogger();
        builder.Services.AddHttpClient("oidc-token");

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
                options.Authority = oidcAuthority;
                options.ClientId = oidcClientId;
                options.ClientSecret = oidcClientSecret;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.RequireHttpsMetadata = isProduction;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.Scope.Add("offline_access");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = rolesClaim,
                };
                options.MapInboundClaims = false;
                options.Events = new OpenIdConnectEvents
                {
                    // Auth0 requires an explicit audience parameter in the authorization request
                    // to return a JWT access token (instead of an opaque token).
                    OnRedirectToIdentityProvider = ctx =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/api"))
                        {
                            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            ctx.HandleResponse();
                            return Task.CompletedTask;
                        }

                        ctx.ProtocolMessage.SetParameter("audience", oidcAudience);
                        return Task.CompletedTask;
                    },

                    // Roles are injected by the Auth0 post-login Action as a namespaced claim
                    // on both the access token and the ID token. Copy them to the identity
                    // as "roles" so authorization policies and /bff/user work uniformly.
                    OnTokenValidated = ctx =>
                    {
                        if (ctx.Principal?.Identity is not ClaimsIdentity identity)
                            return Task.CompletedTask;

                        var accessToken = ctx.TokenEndpointResponse?.AccessToken;
                        if (string.IsNullOrEmpty(accessToken))
                            return Task.CompletedTask;

                        var jwt = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(accessToken);
                        foreach (var c in jwt.Claims.Where(c => c.Type == rolesClaim))
                        {
                            if (!string.IsNullOrEmpty(c.Value) && !identity.HasClaim("roles", c.Value))
                                identity.AddClaim(new Claim("roles", c.Value));
                        }

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
