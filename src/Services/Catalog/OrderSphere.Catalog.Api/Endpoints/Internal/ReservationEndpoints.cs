using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Catalog.Application.Abstractions;
using OrderSphere.Catalog.Domain.Entities;
using OrderSphere.Catalog.Domain.Enums;

namespace OrderSphere.Catalog.Api.Endpoints.Internal;

/// <summary>
/// Stock-reservation saga endpoints consumed by Ordering (checkout reserves, the worker
/// confirms/releases). Not exposed through the public gateway — protected by network policy.
/// </summary>
public static class ReservationEndpoints
{
    private static readonly TimeSpan ReservationTtl = TimeSpan.FromMinutes(30);

    public static void MapInternalReservationEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", Reserve);
        group.MapPost("/{correlationId:guid}/confirm", Confirm);
        group.MapPost("/{correlationId:guid}/release", Release);
    }

    private static async Task<IResult> Reserve(
        ReserveStockRequest body, ICatalogDbContext context, CancellationToken ct)
    {
        if (body.Items is null || body.Items.Count == 0)
            return Results.BadRequest("No items to reserve.");

        var now = DateTime.UtcNow;

        // Idempotent: a prior attempt for this correlation already holds the reservation.
        var alreadyReserved = await context.StockReservations
            .AsNoTracking()
            .AnyAsync(r => r.CorrelationId == body.CorrelationId && r.Status == ReservationStatus.Active, ct);
        if (alreadyReserved)
            return Results.Ok();

        // Availability = on-hand stock − quantity held by non-expired active reservations.
        foreach (var item in body.Items)
        {
            var productId = ProductId.From(item.ProductId);

            var product = await context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId, ct);

            if (product is null)
                return Results.NotFound($"Product {item.ProductId} not found.");

            var activelyReserved = await context.StockReservations
                .AsNoTracking()
                .Where(r => r.ProductId == productId && r.Status == ReservationStatus.Active && r.ExpiresAt > now)
                .SumAsync(r => r.Quantity, ct);

            if (product.Stock - activelyReserved < item.Quantity)
                return Results.Conflict($"Insufficient stock for product {item.ProductId}.");
        }

        var expiresAt = now.Add(ReservationTtl);
        foreach (var item in body.Items)
        {
            context.StockReservations.Add(
                new StockReservation(body.CorrelationId, ProductId.From(item.ProductId), item.Quantity, expiresAt));
        }

        await context.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> Confirm(
        Guid correlationId, ICatalogDbContext context, CancellationToken ct)
    {
        var reservations = await context.StockReservations
            .AsTracking()
            .Where(r => r.CorrelationId == correlationId && r.Status == ReservationStatus.Active)
            .ToListAsync(ct);

        foreach (var reservation in reservations)
        {
            var product = await context.Products
                .AsTracking()
                .FirstOrDefaultAsync(p => p.Id == reservation.ProductId, ct);

            if (product is not null)
            {
                var result = product.RemoveFromStock(reservation.Quantity);
                if (result.IsFailure)
                    return Results.Conflict(result.Error.Description);
            }

            reservation.Confirm();
        }

        await context.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Release(
        Guid correlationId, ICatalogDbContext context, CancellationToken ct)
    {
        var reservations = await context.StockReservations
            .AsTracking()
            .Where(r => r.CorrelationId == correlationId && r.Status == ReservationStatus.Active)
            .ToListAsync(ct);

        foreach (var reservation in reservations)
            reservation.Release();

        await context.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    public sealed record ReserveStockRequest(Guid CorrelationId, List<ReserveStockItem> Items);

    public sealed record ReserveStockItem(Guid ProductId, int Quantity);
}
