using Asp.Versioning;
using OrderSphere.Ordering.Api.Endpoints.Internal;

namespace OrderSphere.Ordering.Api.Endpoints;

public static class EndpointMappingExtensions
{
    public static void MapOrderingEndpoints(this WebApplication app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var v1 = app.MapGroup("api/v{version:apiVersion}")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(1.0)
            .MapToApiVersion(1.0);

        v1.MapOrderEndpoints();
        v1.MapOrderHistoryEndpoints();
        v1.MapCheckoutEndpoints();
        v1.MapCouponEndpoints();
        v1.MapReturnEndpoints();
        v1.MapSagaEndpoints();

        // D4 — internal endpoints require a valid client-credentials token (any authenticated
        // caller); M2M tokens carry no role claims, so no role-based policy is applied here.
        app.MapGroup("internal")
            .RequireAuthorization()
            .MapInternalPurchaseEndpoints();
    }
}
