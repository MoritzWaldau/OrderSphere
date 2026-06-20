using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.DependencyInjection;

public static class OidcAuthenticationExtensions
{
    /// <summary>
    /// Registers JWT Bearer authentication without audience validation.
    /// Intended for gateway/proxy services that forward tokens to downstream APIs.
    /// </summary>
    public static TBuilder AddOrderSphereJwtAuth<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder =>
        AddOrderSphereJwtAuthCore(builder, validateAudience: false);

    /// <summary>
    /// Registers JWT Bearer authentication with audience validation against <c>Oidc:Audience</c>.
    /// The <paramref name="audience"/> parameter is accepted for call-site readability only and is not used;
    /// the effective audience is always read from configuration.
    /// </summary>
    public static TBuilder AddOrderSphereJwtAuth<TBuilder>(
        this TBuilder builder,
        string? audience)
        where TBuilder : IHostApplicationBuilder =>
        AddOrderSphereJwtAuthCore(builder, validateAudience: true);

    private static TBuilder AddOrderSphereJwtAuthCore<TBuilder>(
        this TBuilder builder,
        bool validateAudience)
        where TBuilder : IHostApplicationBuilder
    {
        var authority = builder.Configuration["Oidc:Authority"]
            ?? throw new InvalidOperationException(
                "Oidc:Authority is not configured. " +
                "Set it in appsettings.json or as an environment variable.");

        var rolesClaim = builder.Configuration["Oidc:RolesClaim"] ?? "https://ordersphere.dev/roles";

        string? configuredAudience = null;
        if (validateAudience)
        {
            configuredAudience = builder.Configuration["Oidc:Audience"]
                ?? throw new InvalidOperationException(
                    "Oidc:Audience is not configured. " +
                    "Set it in appsettings.json or as an environment variable.");
        }

        builder.Services.AddAuthorization();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.RequireHttpsMetadata = !IsDevEnvironment(builder);
                options.BackchannelTimeout = TimeSpan.FromSeconds(10);

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = validateAudience,
                    ValidAudiences = configuredAudience is not null ? [configuredAudience] : [],

                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ClockSkew = TimeSpan.FromSeconds(30),

                    NameClaimType = "name",
                    RoleClaimType = rolesClaim,
                };

                options.MapInboundClaims = false;

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("OrderSphere.Security");

                        logger.LogWarning(
                            "JWT validation failed for {Method} {Path}: {ErrorType} — {ErrorMessage}",
                            ctx.HttpContext.Request.Method,
                            ctx.HttpContext.Request.Path,
                            ctx.Exception.GetType().Name,
                            ctx.Exception.Message);

                        return Task.CompletedTask;
                    },

                    OnChallenge = ctx =>
                    {
                        if (ctx.AuthenticateFailure is null)
                        {
                            var logger = ctx.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("OrderSphere.Security");

                            logger.LogWarning(
                                "No bearer token for {Method} {Path} — 401 issued",
                                ctx.HttpContext.Request.Method,
                                ctx.HttpContext.Request.Path);
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        return builder;
    }

    private static bool IsDevEnvironment(IHostApplicationBuilder builder) =>
        string.Equals(
            builder.Environment.EnvironmentName,
            "Development",
            StringComparison.OrdinalIgnoreCase);
}
