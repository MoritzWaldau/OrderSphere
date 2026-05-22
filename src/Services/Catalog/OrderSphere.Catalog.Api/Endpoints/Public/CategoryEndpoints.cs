using MediatR;
using OrderSphere.Catalog.Application.Features.Categories.Public.GetCategories;

namespace OrderSphere.Catalog.Api.Endpoints.Public;

public static class CategoryEndpoints
{
    public static void MapPublicCategoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetCategories)
            .WithName("GetCategories")
            .WithTags("Categories");
    }

    private static async Task<IResult> GetCategories(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCategoriesQuery(), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error.Description);
    }
}
