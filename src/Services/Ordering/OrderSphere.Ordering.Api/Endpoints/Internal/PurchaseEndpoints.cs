using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Domain.Enums;

namespace OrderSphere.Ordering.Api.Endpoints.Internal;

/// <summary>
/// Service-to-service endpoints consumed by Catalog.Api (review eligibility).
/// Not exposed through the public API gateway. D4 — the mounting route group requires a
/// valid client-credentials (M2M) bearer token; see <c>EndpointMappingExtensions</c>.
/// </summary>
public static class PurchaseEndpoints
{
    public static void MapInternalPurchaseEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/customers/{customerId:guid}/purchased/{productId:guid}", HasPurchased);
    }

    private static async Task<IResult> HasPurchased(
        Guid customerId,
        Guid productId,
        IOrderingDbContext context,
        CancellationToken ct)
    {
        var typedCustomer = CustomerId.From(customerId);
        var typedProduct = ProductId.From(productId);

        var purchased = await context.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == typedCustomer && o.Status != OrderStatus.Cancelled)
            .AnyAsync(o => o.Items.Any(i => i.ProductId == typedProduct), ct);

        return Results.Ok(purchased);
    }
}
