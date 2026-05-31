namespace OrderSphere.Catalog.Api.Configuration;

/// <summary>
/// Authorization policies for the Catalog API.
/// <list type="bullet">
///   <item><see cref="CatalogAdminPolicy"/> — requires the <c>catalog-admin</c> or <c>admin</c> role.
///     Applied to all write endpoints (create, update, delete) for products and categories.</item>
/// </list>
/// Read endpoints remain anonymous (public catalog browse).
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>Policy name for catalog write access.</summary>
    public const string CatalogAdminPolicy = "CatalogAdminPolicy";

    /// <summary>Legacy constant kept for backward compatibility.</summary>
    public const string AdminPolicy = CatalogAdminPolicy;

    public static IServiceCollection AddCatalogAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(CatalogAdminPolicy, policy =>
                policy.RequireRole("catalog-admin", "admin"));

        return services;
    }
}
