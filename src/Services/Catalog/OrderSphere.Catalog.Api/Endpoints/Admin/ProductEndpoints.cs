using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Catalog.Application.DTOs.Admin;
using OrderSphere.Catalog.Application.Features.Products.Admin.CreateProduct;
using OrderSphere.Catalog.Application.Features.Products.Admin.DeleteProduct;
using OrderSphere.Catalog.Application.Features.Products.Admin.GetAllProductsAdmin;
using OrderSphere.Catalog.Application.Features.Products.Admin.GetProductByIdAdmin;
using OrderSphere.Catalog.Application.Features.Products.Admin.ReindexSearch;
using OrderSphere.Catalog.Application.Features.Products.Admin.UpdateProduct;
using OrderSphere.Catalog.Application.Features.Products.Admin.UploadProductImage;
using OrderSphere.ServiceDefaults;

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

        group.MapPost("/reindex-search", ReindexSearch)
            .WithName("AdminReindexProductSearch")
            .WithTags("Products Admin");

        group.MapPost("/{id:guid}/image", UploadImage)
            .WithName("AdminUploadProductImage")
            .WithTags("Products Admin")
            .DisableAntiforgery();
    }

    private static async Task<IResult> GetAllAdmin(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllProductsAdminQuery(), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetByIdAdmin(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetProductByIdAdminQuery(ProductId.From(id)), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Create(
        [FromBody] AdminProductInput input, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateProductCommand(
                input.Name, input.Description, input.Price,
                input.Stock, CategoryId.From(input.CategoryId), input.SKU, input.ImageUrl,
                input.BrandId is { } brandId ? BrandId.From(brandId) : null), ct);

        return result.ToHttpResult(
            id => Results.Created($"/api/v1/admin/products/{id}", new { id }));
    }

    private static async Task<IResult> Update(
        Guid id, [FromBody] AdminProductInput input, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateProductCommand(
                ProductId.From(id), input.Name, input.Description, input.Price,
                input.Stock, CategoryId.From(input.CategoryId), input.SKU, input.IsActive, input.ImageUrl,
                input.BrandId is { } brandId ? BrandId.From(brandId) : null), ct);

        return result.ToHttpResult();
    }

    private static async Task<IResult> Delete(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteProductCommand(ProductId.From(id)), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ReindexSearch(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ReindexSearchCommand(), ct);
        return result.ToHttpResult(indexed => Results.Ok(new { indexed }));
    }

    private static async Task<IResult> UploadImage(
        Guid id, IFormFile file, IMediator mediator, CancellationToken ct)
    {
        if (file.Length > 5 * 1024 * 1024)
            return Results.Problem("Image must be 5 MB or smaller.", statusCode: 400);

        await using var stream = file.OpenReadStream();
        var result = await mediator.Send(
            new UploadProductImageCommand(ProductId.From(id), stream, file.ContentType, file.FileName), ct);

        return result.ToHttpResult(blobName => Results.Ok(new { blobName }));
    }
}
