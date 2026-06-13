using Asp.Versioning;

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
            .HasApiVersion(1.0);

        v1.MapOrderEndpoints();
        v1.MapCheckoutEndpoints();
        v1.MapCouponEndpoints();
    }
}
