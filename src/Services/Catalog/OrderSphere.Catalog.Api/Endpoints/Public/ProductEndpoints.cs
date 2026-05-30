using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Catalog.Application.Abstractions;
using OrderSphere.Catalog.Application.Features.Products.Public.GetProductBySlug;
using OrderSphere.Catalog.Application.Features.Products.Public.GetProducts;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Catalog.Api.Endpoints.Public;

public static class ProductEndpoints
{
    public static void MapPublicProductEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetProducts)
            .WithName("GetProducts")
            .WithTags("Products");

        group.MapGet("/batch", GetProductsByIds)
            .WithName("GetProductsByIds")
            .WithTags("Products");

        group.MapGet("/{slug}", GetProductBySlug)
            .WithName("GetProductBySlug")
            .WithTags("Products");

        group.MapPost("/{id:guid}/stock/decrement", DecrementStock)
            .WithName("DecrementStock")
            .WithTags("Products");

        group.MapPost("/{id:guid}/stock/restore", RestoreStock)
            .WithName("RestoreStock")
            .WithTags("Products");
    }

    private static async Task<IResult> GetProducts(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetProductsQuery(page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize, search, categoryId), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetProductsByIds(
        [FromQuery] string ids,
        IMediator mediator,
        CancellationToken ct)
    {
        var productIds = ids.Split(',')
            .Select(s => Guid.TryParse(s.Trim(), out var g) ? (Guid?)g : null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();

        var result = await mediator.Send(new GetProductsQuery(1, int.MaxValue), ct);
        if (result.IsFailure) return result.ToHttpResult();

        var filtered = result.Value.Items.Where(p => productIds.Contains(p.Id));
        return Results.Ok(filtered);
    }

    private static async Task<IResult> GetProductBySlug(
        string slug, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetProductBySlugQuery(slug), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DecrementStock(
        Guid id, [FromBody] StockChangeRequest body, ICatalogDbContext context, CancellationToken ct)
    {
        var product = await context.Products
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == ProductId.From(id) && !p.IsDeleted, ct);

        if (product is null) return Results.NotFound();

        var result = product.RemoveFromStock(body.Quantity);
        if (result.IsFailure) return result.ToHttpResult();

        await context.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RestoreStock(
        Guid id, [FromBody] StockChangeRequest body, ICatalogDbContext context, CancellationToken ct)
    {
        var product = await context.Products
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == ProductId.From(id) && !p.IsDeleted, ct);

        if (product is null) return Results.NotFound();

        product.AddToStock(body.Quantity);
        await context.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private sealed record StockChangeRequest(int Quantity);
}
