namespace OrderSphere.Catalog.Api.Configuration;

/// <summary>
/// Authentication wiring for the Catalog API.
/// Delegates to the shared <c>AddOrderSphereJwtAuth</c> extension in ServiceDefaults
/// so that all JWT validation parameters stay consistent across the solution.
/// Audience "catalog-api" maps to a dedicated bearer-only Keycloak client.
/// </summary>
public static class AuthenticationExtensions
{
    public static IHostApplicationBuilder AddCatalogAuthentication(
        this IHostApplicationBuilder builder)
    {
        builder.AddOrderSphereJwtAuth("catalog-api");
        return builder;
    }
}
