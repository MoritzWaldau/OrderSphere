using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderSphere.Catalog.Api.Features.Categories;
using OrderSphere.Catalog.Api.Features.Categories.Admin;
using OrderSphere.Catalog.Api.Models.Admin;

namespace OrderSphere.Catalog.Api.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/categories", GetCategories);

        var admin = app.MapGroup("/api/v1/admin/categories").RequireAuthorization("AdminPolicy");
        admin.MapGet("/", GetAllAdmin);
        admin.MapPost("/", Create);
        admin.MapPut("/{id:guid}", Update);
        admin.MapDelete("/{id:guid}", Delete);
    }

    private static async Task<IResult> GetCategories(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCategoriesQuery(), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error.Description);
    }

    private static async Task<IResult> GetAllAdmin(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllCategoriesAdminQuery(), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error.Description);
    }

    private static async Task<IResult> Create([FromBody] AdminCategoryInput input, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateCategoryCommand(input.Name, input.Description), ct);
        return result.IsSuccess
            ? Results.Created($"/api/v1/admin/categories/{result.Value}", new { id = result.Value })
            : Results.Problem(result.Error.Description);
    }

    private static async Task<IResult> Update(Guid id, [FromBody] AdminCategoryInput input, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateCategoryCommand(id, input.Name, input.Description, input.IsActive), ct);
        return result.IsSuccess ? Results.NoContent() : Results.Problem(result.Error.Description);
    }

    private static async Task<IResult> Delete(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteCategoryCommand(id, string.Empty), ct);
        return result.IsSuccess ? Results.NoContent() : Results.Problem(result.Error.Description);
    }
}
