using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Catalog.Application.DTOs.Admin;
using OrderSphere.Catalog.Application.Features.Categories.Admin.CreateCategory;
using OrderSphere.Catalog.Application.Features.Categories.Admin.DeleteCategory;
using OrderSphere.Catalog.Application.Features.Categories.Admin.GetAllCategoriesAdmin;
using OrderSphere.Catalog.Application.Features.Categories.Admin.UpdateCategory;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Catalog.Api.Endpoints.Admin;

public static class CategoryEndpoints
{
    public static void MapAdminCategoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllAdmin)
            .WithName("AdminGetAllCategories")
            .WithTags("Categories Admin");

        group.MapPost("/", Create)
            .WithName("AdminCreateCategory")
            .WithTags("Categories Admin");

        group.MapPut("/{id:guid}", Update)
            .WithName("AdminUpdateCategory")
            .WithTags("Categories Admin");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("AdminDeleteCategory")
            .WithTags("Categories Admin");
    }

    private static async Task<IResult> GetAllAdmin(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllCategoriesAdminQuery(), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Create(
        [FromBody] AdminCategoryInput input, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateCategoryCommand(input.Name, input.Description), ct);
        return result.ToHttpResult(
            id => Results.Created($"/api/v1/admin/categories/{id}", new { id }));
    }

    private static async Task<IResult> Update(
        Guid id, [FromBody] AdminCategoryInput input, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateCategoryCommand(CategoryId.From(id), input.Name, input.Description, input.IsActive), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Delete(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteCategoryCommand(CategoryId.From(id)), ct);
        return result.ToHttpResult();
    }
}
