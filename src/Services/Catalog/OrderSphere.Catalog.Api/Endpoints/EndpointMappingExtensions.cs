using Asp.Versioning;
using OrderSphere.Catalog.Api.Configuration;
using OrderSphere.Catalog.Api.Endpoints.Admin;
using OrderSphere.Catalog.Api.Endpoints.Internal;
using OrderSphere.Catalog.Api.Endpoints.Public;
using OrderSphere.Catalog.Api.Grpc;

namespace OrderSphere.Catalog.Api.Endpoints;

public static class EndpointMappingExtensions
{
    public static WebApplication MapCatalogEndpoints(this WebApplication app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var v1 = app.MapGroup("api/v{version:apiVersion}")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(1.0);

        v1.MapGroup("products")
            .RequireRateLimiting(RateLimitingExtensions.PublicPolicy)
            .MapPublicProductEndpoints();

        v1.MapGroup("categories")
            .RequireRateLimiting(RateLimitingExtensions.PublicPolicy)
            .MapPublicCategoryEndpoints();

        v1.MapGroup("admin/products")
            // .RequireAuthorization(AuthorizationExtensions.AdminPolicy)   ← Auth-Anker
            .RequireRateLimiting(RateLimitingExtensions.AdminPolicy)
            .MapAdminProductEndpoints();

        v1.MapGroup("admin/categories")
            // .RequireAuthorization(AuthorizationExtensions.AdminPolicy)   ← Auth-Anker
            .RequireRateLimiting(RateLimitingExtensions.AdminPolicy)
            .MapAdminCategoryEndpoints();

        app.MapGroup("internal/products")
            .MapInternalProductEndpoints();

        app.MapGrpcService<CatalogGrpcService>();

        return app;
    }
}
