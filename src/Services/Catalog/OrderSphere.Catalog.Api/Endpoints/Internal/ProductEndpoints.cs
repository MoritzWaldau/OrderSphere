using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Catalog.Application.Abstractions;

namespace OrderSphere.Catalog.Api.Endpoints.Internal;

/// <summary>
/// Service-to-service endpoints consumed by Ordering.Api.
/// Not exposed through the public API gateway — protected by network policy at the cluster level.
/// </summary>
public static class ProductEndpoints
{
    public static void MapInternalProductEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{productId:guid}", GetProductById);
        group.MapGet("/names", GetProductNamesByIds);
        group.MapGet("/infos", GetProductInfosByIds);
        group.MapPost("/{productId:guid}/decrement-stock", DecrementStock);
        group.MapPost("/{productId:guid}/restore-stock", RestoreStock);
    }

    private static async Task<IResult> GetProductById(
        Guid productId,
        ICatalogDbContext context,
        CancellationToken ct)
    {
        var product = await context.Products
            .AsNoTracking()
            .Where(p => p.Id == ProductId.From(productId))
            .Select(p => new InternalProductDto(p.Id.Value, p.Name, p.Price.Amount, p.Stock, p.IsActive, p.CategoryId.Value))
            .FirstOrDefaultAsync(ct);

        return product is null ? Results.NotFound() : Results.Ok(product);
    }

    private static async Task<IResult> GetProductNamesByIds(
        [FromQuery(Name = "ids")] Guid[] ids,
        ICatalogDbContext context,
        CancellationToken ct)
    {
        if (ids.Length == 0)
            return Results.Ok(new Dictionary<Guid, string>());

        var typedIds = ids.Select(ProductId.From).ToList();
        var names = await context.Products
            .AsNoTracking()
            .Where(p => typedIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(ct);

        return Results.Ok(names.ToDictionary(p => p.Id.Value, p => p.Name));
    }

    private static async Task<IResult> GetProductInfosByIds(
        [FromQuery(Name = "ids")] Guid[] ids,
        ICatalogDbContext context,
        CancellationToken ct)
    {
        if (ids.Length == 0)
            return Results.Ok(new Dictionary<Guid, InternalProductDto>());

        var typedIds = ids.Select(ProductId.From).ToList();
        var products = await context.Products
            .AsNoTracking()
            .Where(p => typedIds.Contains(p.Id))
            .Select(p => new InternalProductDto(p.Id.Value, p.Name, p.Price.Amount, p.Stock, p.IsActive, p.CategoryId.Value))
            .ToListAsync(ct);

        return Results.Ok(products.ToDictionary(p => p.Id));
    }

    private static async Task<IResult> DecrementStock(
        Guid productId,
        [FromBody] StockChangeRequest body,
        ICatalogDbContext context,
        CancellationToken ct)
    {
        var product = await context.Products
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == ProductId.From(productId), ct);

        if (product is null) return Results.NotFound();

        var result = product.RemoveFromStock(body.Quantity);
        if (result.IsFailure) return Results.Conflict(result.Error.Description);

        await context.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RestoreStock(
        Guid productId,
        [FromBody] StockChangeRequest body,
        ICatalogDbContext context,
        CancellationToken ct)
    {
        var product = await context.Products
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == ProductId.From(productId), ct);

        if (product is null) return Results.NotFound();

        product.AddToStock(body.Quantity);
        await context.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private sealed record InternalProductDto(Guid Id, string Name, decimal Price, int Stock, bool IsActive, Guid CategoryId = default);

    private sealed record StockChangeRequest(int Quantity);
}
