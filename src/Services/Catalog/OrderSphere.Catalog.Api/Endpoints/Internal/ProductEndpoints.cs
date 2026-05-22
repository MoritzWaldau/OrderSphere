using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            .Where(p => p.Id == productId && !p.IsDeleted)
            .Select(p => new InternalProductDto(p.Id, p.Name, p.Price, p.Stock, p.IsActive))
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

        var names = await context.Products
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id) && !p.IsDeleted)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(ct);

        return Results.Ok(names.ToDictionary(p => p.Id, p => p.Name));
    }

    private static async Task<IResult> DecrementStock(
        Guid productId,
        [FromBody] StockChangeRequest body,
        ICatalogDbContext context,
        CancellationToken ct)
    {
        var product = await context.Products
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted, ct);

        if (product is null) return Results.NotFound();
        if (product.Stock < body.Quantity) return Results.Conflict("Insufficient stock.");

        product.RemoveFromStock(body.Quantity);
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
            .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted, ct);

        if (product is null) return Results.NotFound();

        product.AddToStock(body.Quantity);
        await context.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private sealed record InternalProductDto(Guid Id, string Name, decimal Price, int Stock, bool IsActive);

    private sealed record StockChangeRequest(int Quantity);
}
