using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Catalog.Application.DTOs.Admin;
using OrderSphere.Catalog.Application.Features.Brands.Admin.CreateBrand;
using OrderSphere.Catalog.Application.Features.Brands.Admin.DeleteBrand;
using OrderSphere.Catalog.Application.Features.Brands.Admin.GetAllBrandsAdmin;
using OrderSphere.Catalog.Application.Features.Brands.Admin.UpdateBrand;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Catalog.Api.Endpoints.Admin;

public static class BrandEndpoints
{
    public static void MapAdminBrandEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllAdmin)
            .WithName("AdminGetAllBrands")
            .WithTags("Brands Admin");

        group.MapPost("/", Create)
            .WithName("AdminCreateBrand")
            .WithTags("Brands Admin");

        group.MapPut("/{id:guid}", Update)
            .WithName("AdminUpdateBrand")
            .WithTags("Brands Admin");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("AdminDeleteBrand")
            .WithTags("Brands Admin");
    }

    private static async Task<IResult> GetAllAdmin(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllBrandsAdminQuery(), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Create(
        [FromBody] AdminBrandInput input, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateBrandCommand(input.Name, input.Description, input.LogoUrl), ct);
        return result.ToHttpResult(
            id => Results.Created($"/api/v1/admin/brands/{id}", new { id }));
    }

    private static async Task<IResult> Update(
        Guid id, [FromBody] AdminBrandInput input, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateBrandCommand(BrandId.From(id), input.Name, input.Description, input.LogoUrl, input.IsActive), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Delete(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteBrandCommand(BrandId.From(id)), ct);
        return result.ToHttpResult();
    }
}
