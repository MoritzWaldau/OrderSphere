using Microsoft.EntityFrameworkCore;
using OrderSphere.Basket.Api.Models;
using OrderSphere.Basket.Infrastructure.Persistence;

namespace OrderSphere.Basket.Api.Endpoints;

public static class InternalCartEndpoints
{
    public static void MapInternalCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/internal/cart");

        group.MapGet("/{customerId:guid}", async (Guid customerId, BasketDbContext context, CancellationToken ct) =>
        {
            var cart = await context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);

            if (cart is null)
                return Results.NotFound();

            var dto = new CartDto(
                cart.CustomerId,
                cart.Items.Select(ci => new CartItemDto(ci.ProductId, "", 0m, ci.Quantity)).ToList());

            return Results.Ok(dto);
        });

        group.MapDelete("/{customerId:guid}/items", async (Guid customerId, BasketDbContext context, CancellationToken ct) =>
        {
            var cart = await context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);

            if (cart is null)
                return Results.NotFound();

            context.CartItems.RemoveRange(cart.Items);
            await context.SaveChangesAsync(ct);

            return Results.NoContent();
        });
    }
}
