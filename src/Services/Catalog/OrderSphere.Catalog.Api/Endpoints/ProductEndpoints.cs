using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Catalog.Api.Features.Products;
using OrderSphere.Catalog.Api.Features.Products.Admin;
using OrderSphere.Catalog.Api.Models;
using OrderSphere.Catalog.Api.Models.Admin;
using OrderSphere.Catalog.Infrastructure.Persistence;

namespace OrderSphere.Catalog.Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var pub = app.MapGroup("/api/v1/products");
        pub.MapGet("/", GetProducts);
        pub.MapGet("/batch", GetProductsByIds);
        pub.MapGet("/{slug}", GetProductBySlug);
        pub.MapPost("/{id:guid}/stock/decrement", DecrementStock);
        pub.MapPost("/{id:guid}/stock/restore", RestoreStock);

        var admin = app.MapGroup("/api/v1/admin/products").RequireAuthorization("AdminPolicy");
        admin.MapGet("/", GetAllAdmin);
        admin.MapGet("/{id:guid}", GetByIdAdmin);
        admin.MapPost("/", Create);
        admin.MapPut("/{id:guid}", Update);
        admin.MapDelete("/{id:guid}", Delete);
    }

    private static async Task<IResult> GetProducts(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetProductsQuery(), ct);
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

        var result = await mediator.Send(new GetProductsQuery(), ct);
        if (!result.IsSuccess) return Results.Problem(result.Error.Description);

        var filtered = result.Value.Where(p => productIds.Contains(p.Id));
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
        Guid id, [FromBody] StockChangeRequest body, CatalogDbContext context, CancellationToken ct)
    {
        var product = await context.Products.AsTracking().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (product is null) return Results.NotFound();
        if (product.Stock < body.Quantity) return Results.Conflict("Insufficient stock.");
        product.RemoveFromStock(body.Quantity);
        await context.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RestoreStock(
        Guid id, [FromBody] StockChangeRequest body, CatalogDbContext context, CancellationToken ct)
    {
        var product = await context.Products.AsTracking().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (product is null) return Results.NotFound();
        product.AddToStock(body.Quantity);
        await context.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private sealed record StockChangeRequest(int Quantity);
}
