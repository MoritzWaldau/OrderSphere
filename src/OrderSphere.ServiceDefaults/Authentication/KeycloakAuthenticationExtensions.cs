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
    /// <summary>
    /// Overload for gateway/proxy services that forward tokens to downstream APIs.
    /// Audience validation is skipped; each downstream service validates its own audience.
    /// </summary>
    public static TBuilder AddOrderSphereJwtAuth<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder =>
        AddOrderSphereJwtAuth(builder, audience: null);

    public static TBuilder AddOrderSphereJwtAuth<TBuilder>(
        this TBuilder builder,
        string? audience)
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
                    ValidateAudience = audience is not null,
                    ValidAudiences = audience is not null ? [audience] : [],

                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ClockSkew = TimeSpan.FromSeconds(30),

                    NameClaimType = "preferred_username",
                    RoleClaimType = "roles",
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
