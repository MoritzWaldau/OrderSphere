using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Catalog.Application.Abstractions;
using OrderSphere.Catalog.Application.DTOs.Admin;
using OrderSphere.Catalog.Application.Features.Products;
using OrderSphere.Catalog.Application.Features.Products.Admin;

namespace OrderSphere.Catalog.Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapPublicProductEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetProducts)
            .WithName("GetProducts")
            .WithTags("Products")
            .WithOpenApi();

        group.MapGet("/batch", GetProductsByIds)
            .WithName("GetProductsByIds")
            .WithTags("Products")
            .WithOpenApi();

        group.MapGet("/{slug}", GetProductBySlug)
            .WithName("GetProductBySlug")
            .WithTags("Products")
            .WithOpenApi();

        group.MapPost("/{id:guid}/stock/decrement", DecrementStock)
            .WithName("DecrementStock")
            .WithTags("Products")
            .WithOpenApi();

        group.MapPost("/{id:guid}/stock/restore", RestoreStock)
            .WithName("RestoreStock")
            .WithTags("Products")
            .WithOpenApi();
    }

    public static void MapAdminProductEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllAdmin)
            .WithName("AdminGetAllProducts")
            .WithTags("Products Admin")
            .WithOpenApi();

        group.MapGet("/{id:guid}", GetByIdAdmin)
            .WithName("AdminGetProductById")
            .WithTags("Products Admin")
            .WithOpenApi();

        group.MapPost("/", Create)
            .WithName("AdminCreateProduct")
            .WithTags("Products Admin")
            .WithOpenApi();

        group.MapPut("/{id:guid}", Update)
            .WithName("AdminUpdateProduct")
            .WithTags("Products Admin")
            .WithOpenApi();

        group.MapDelete("/{id:guid}", Delete)
            .WithName("AdminDeleteProduct")
            .WithTags("Products Admin")
            .WithOpenApi();
    }

    private static async Task<IResult> GetProducts(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetProductsQuery(page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error.Description);
    }

    private static async Task<IResult> GetProductsByIds(
        [FromQuery] string ids,
        IMediator mediator,
        CancellationToken ct)
    {
        var productIds = ids.Split(',')
            .Select(s => Guid.TryParse(s.Trim(), out var g) ? (Guid?)g : null)
            .Where(g => g.HasValue).Select(g => g!.Value)
            .ToList();

        var result = await mediator.Send(new GetProductsQuery(1, int.MaxValue), ct);
        if (!result.IsSuccess) return Results.Problem(result.Error.Description);

        var filtered = result.Value.Items.Where(p => productIds.Contains(p.Id));
        return Results.Ok(filtered);
    }

    private static async Task<IResult> GetProductBySlug(string slug, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetProductBySlugQuery(slug), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
    }

    private static async Task<IResult> GetAllAdmin(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllProductsAdminQuery(), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error.Description);
    }

    private static async Task<IResult> GetByIdAdmin(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetProductByIdAdminQuery(id), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
    }

    private static async Task<IResult> Create([FromBody] AdminProductInput input, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateProductCommand(input.Name, input.Description, input.Price, input.Stock, input.CategoryId, input.SKU), ct);
        return result.IsSuccess
            ? Results.Created($"/api/v1/admin/products/{result.Value}", new { id = result.Value })
            : Results.Problem(result.Error.Description);
    }

    private static async Task<IResult> Update(Guid id, [FromBody] AdminProductInput input, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateProductCommand(id, input.Name, input.Description, input.Price, input.Stock, input.CategoryId, input.SKU, input.IsActive), ct);
        return result.IsSuccess ? Results.NoContent() : Results.Problem(result.Error.Description);
    }

    private static async Task<IResult> Delete(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteProductCommand(id), ct);
        return result.IsSuccess ? Results.NoContent() : Results.Problem(result.Error.Description);
    }

    private static async Task<IResult> DecrementStock(
        Guid id, [FromBody] StockChangeRequest body, ICatalogDbContext context, CancellationToken ct)
    {
        var product = await context.Products.AsTracking().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (product is null) return Results.NotFound();
        if (product.Stock < body.Quantity) return Results.Conflict("Insufficient stock.");
        product.RemoveFromStock(body.Quantity);
        await context.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RestoreStock(
        Guid id, [FromBody] StockChangeRequest body, ICatalogDbContext context, CancellationToken ct)
    {
        var product = await context.Products.AsTracking().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (product is null) return Results.NotFound();
        product.AddToStock(body.Quantity);
        await context.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private sealed record StockChangeRequest(int Quantity);
}
