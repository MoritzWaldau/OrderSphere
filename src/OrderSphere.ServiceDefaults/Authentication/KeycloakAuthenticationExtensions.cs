using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Shared Keycloak JWT authentication wiring for every API in the solution.
/// All validation parameters live here so each service does not diverge.
/// </summary>
public static class KeycloakAuthenticationExtensions
{
    /// <summary>
    /// Registers JWT Bearer authentication against the configured Keycloak realm.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="audience">
    ///   The audience value that must appear in the token's <c>aud</c> claim.
    ///   Each service owns a dedicated bearer-only Keycloak client
    ///   (e.g. <c>ordering-api</c>, <c>catalog-api</c>, <c>userprofile-api</c>).
    /// </param>
    public static TBuilder AddOrderSphereJwtAuth<TBuilder>(
        this TBuilder builder,
        string audience)
        where TBuilder : IHostApplicationBuilder
    {
        var authority = builder.Configuration["Keycloak:Authority"]
            ?? throw new InvalidOperationException(
                "Keycloak:Authority is not configured. " +
                "Set it in appsettings.json or as an environment variable.");

        builder.Services.AddAuthorization();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.RequireHttpsMetadata = !IsDevEnvironment(builder);

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Audience: each API validates only tokens issued for itself.
                    ValidateAudience = true,
                    ValidAudiences = [audience],

                    ValidateIssuer = true,
                    // Issuer is derived from Authority by the OIDC discovery document;
                    // no hard-coded string needed here.

                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    // Allow up to 30 s clock drift between servers.
                    ClockSkew = TimeSpan.FromSeconds(30),

                    // Map Keycloak claim names to .NET identity claim types.
                    NameClaimType = "preferred_username",
                    RoleClaimType = "roles",
                };

                // Do not translate Keycloak claim names to legacy WS-Federation types.
                options.MapInboundClaims = false;

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        // Log failure without echoing token content.
                        var logger = ctx.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("OrderSphere.Security");

                        logger.LogWarning(
                            "JWT authentication failed for {Path}: {ErrorType}",
                            ctx.HttpContext.Request.Path,
                            ctx.Exception.GetType().Name);

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
