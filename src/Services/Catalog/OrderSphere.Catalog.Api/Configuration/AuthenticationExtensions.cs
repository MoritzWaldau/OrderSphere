namespace OrderSphere.Catalog.Api.Configuration;

/// <summary>
/// Authentication wiring for the Catalog API.
/// Currently a no-op placeholder. Wire up JWT/Keycloak here when authentication is reintroduced.
/// Expected pattern:
///   services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
///       .AddJwtBearer(options => { /* Keycloak realm, audience, validation params */ });
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddCatalogAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Intentionally empty until authentication is reactivated.
        return services;
    }
}
