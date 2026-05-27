using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Catalog.Application.DTOs.Admin;
using OrderSphere.Catalog.Application.Features.Products.Admin.CreateProduct;
using OrderSphere.Catalog.Application.Features.Products.Admin.DeleteProduct;
using OrderSphere.Catalog.Application.Features.Products.Admin.GetAllProductsAdmin;
using OrderSphere.Catalog.Application.Features.Products.Admin.GetProductByIdAdmin;
using OrderSphere.Catalog.Application.Features.Products.Admin.UpdateProduct;

namespace OrderSphere.Catalog.Api.Endpoints.Admin;

public static class ProductEndpoints
{
    public static void MapAdminProductEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllAdmin)
            .WithName("AdminGetAllProducts")
            .WithTags("Products Admin");

        group.MapGet("/{id:guid}", GetByIdAdmin)
            .WithName("AdminGetProductById")
            .WithTags("Products Admin");

        group.MapPost("/", Create)
            .WithName("AdminCreateProduct")
            .WithTags("Products Admin");

        group.MapPut("/{id:guid}", Update)
            .WithName("AdminUpdateProduct")
            .WithTags("Products Admin");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("AdminDeleteProduct")
            .WithTags("Products Admin");
    }

    private static async Task<IResult> GetAllAdmin(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllProductsAdminQuery(), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error.Description);
    }

    private static async Task<IResult> GetByIdAdmin(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetProductByIdAdminQuery(ProductId.From(id)), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
    }

    private static async Task<IResult> Create(
        [FromBody] AdminProductInput input, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateProductCommand(
                input.Name, input.Description, input.Price,
                input.Stock, CategoryId.From(input.CategoryId), input.SKU, input.ImageUrl), ct);

        return result.IsSuccess
            ? Results.Created($"/api/v1/admin/products/{result.Value}", new { id = result.Value })
            : Results.Problem(result.Error.Description);
    }

    private static async Task<IResult> Update(
        Guid id, [FromBody] AdminProductInput input, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateProductCommand(
                ProductId.From(id), input.Name, input.Description, input.Price,
                input.Stock, CategoryId.From(input.CategoryId), input.SKU, input.IsActive, input.ImageUrl), ct);

        return result.IsSuccess ? Results.NoContent() : Results.Problem(result.Error.Description);
    }

    private static async Task<IResult> Delete(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteProductCommand(ProductId.From(id)), ct);
        return result.IsSuccess ? Results.NoContent() : Results.Problem(result.Error.Description);
    }
}
