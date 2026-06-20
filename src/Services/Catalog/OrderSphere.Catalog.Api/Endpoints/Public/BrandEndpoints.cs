using MediatR;
using OrderSphere.Catalog.Application.Features.Brands.Public.GetBrands;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Catalog.Api.Endpoints.Public;

public static class BrandEndpoints
{
    public static void MapPublicBrandEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetBrands)
            .WithName("GetBrands")
            .WithTags("Brands");
    }

    private static async Task<IResult> GetBrands(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetBrandsQuery(), ct);
        return result.ToHttpResult();
    }
}
