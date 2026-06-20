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
            .HasApiVersion(1.0)
            .MapToApiVersion(1.0);

        v1.MapGroup("products")
            .RequireRateLimiting(RateLimitingExtensions.PublicPolicy)
            .MapPublicProductEndpoints();

        v1.MapGroup("categories")
            .RequireRateLimiting(RateLimitingExtensions.PublicPolicy)
            .MapPublicCategoryEndpoints();

        v1.MapGroup("brands")
            .RequireRateLimiting(RateLimitingExtensions.PublicPolicy)
            .MapPublicBrandEndpoints();

        v1.MapGroup("reviews")
            .RequireRateLimiting(RateLimitingExtensions.PublicPolicy)
            .MapPublicReviewEndpoints();

        v1.MapGroup("admin/products")
            .RequireAuthorization(AuthorizationExtensions.CatalogAdminPolicy)
            .RequireRateLimiting(RateLimitingExtensions.AdminPolicy)
            .MapAdminProductEndpoints();

        v1.MapGroup("admin/categories")
            .RequireAuthorization(AuthorizationExtensions.CatalogAdminPolicy)
            .RequireRateLimiting(RateLimitingExtensions.AdminPolicy)
            .MapAdminCategoryEndpoints();

        v1.MapGroup("admin/brands")
            .RequireAuthorization(AuthorizationExtensions.CatalogAdminPolicy)
            .RequireRateLimiting(RateLimitingExtensions.AdminPolicy)
            .MapAdminBrandEndpoints();

        v1.MapGroup("admin/reviews")
            .RequireAuthorization(AuthorizationExtensions.CatalogAdminPolicy)
            .RequireRateLimiting(RateLimitingExtensions.AdminPolicy)
            .MapAdminReviewEndpoints();

        app.MapGroup("internal/products")
            .MapInternalProductEndpoints();

        app.MapGroup("internal/reservations")
            .MapInternalReservationEndpoints();

        app.MapGrpcService<CatalogGrpcService>();

        return app;
    }
}
