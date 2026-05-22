namespace OrderSphere.Catalog.Api.Configuration;

/// <summary>
/// Authorization policies for the Catalog API.
/// Currently a no-op placeholder. AdminPolicy constant is defined so that
/// EndpointMappingExtensions can reference it without a magic string.
/// When reactivated:
///   services.AddAuthorizationBuilder()
///       .AddPolicy(AdminPolicy, p => p.RequireRole("admin"));
/// </summary>
public static class AuthorizationExtensions
{
    public const string AdminPolicy = "AdminPolicy";

    public static IServiceCollection AddCatalogAuthorization(this IServiceCollection services)
    {
        // Intentionally empty until authorization is reactivated.
        return services;
    }
}
