using MediatR;
using OrderSphere.BuildingBlocks.Security;
using OrderSphere.Ordering.Api.Features.Cart;
using OrderSphere.Ordering.Api.Models;

namespace OrderSphere.Ordering.Api.Endpoints;

public static class CartEndpoints
{
    public static void MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/cart").RequireAuthorization();

        // GET /api/v1/cart — returns the cart for the authenticated customer.
        group.MapGet("/", async (ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetCustomerId(currentUser, out var customerId))
                return Results.Unauthorized();

            var result = await mediator.Send(new GetCartQuery(customerId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new ErrorResponse(result.Error.Code, result.Error.Description));
        });

        // POST /api/v1/cart/add — adds a product to the authenticated customer's cart.
        group.MapPost("/add", async (AddToCartRequest req, ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetCustomerId(currentUser, out var customerId))
                return Results.Unauthorized();

            var result = await mediator.Send(new AddToCartCommand(customerId, req.ProductId, req.Quantity), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new ErrorResponse(result.Error.Code, result.Error.Description));
        });

        // DELETE /api/v1/cart/remove — removes a product from the authenticated customer's cart.
        group.MapDelete("/remove", async (RemoveFromCartRequest req, ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetCustomerId(currentUser, out var customerId))
                return Results.Unauthorized();

            var result = await mediator.Send(new RemoveFromCartCommand(customerId, req.ProductId), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new ErrorResponse(result.Error.Code, result.Error.Description));
        });

        // PUT /api/v1/cart/decrease — decreases quantity for a product in the authenticated customer's cart.
        group.MapPut("/decrease", async (DecreaseCartItemRequest req, ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetCustomerId(currentUser, out var customerId))
                return Results.Unauthorized();

            var result = await mediator.Send(new DecreaseCartItemQuantityCommand(customerId, req.ProductId), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new ErrorResponse(result.Error.Code, result.Error.Description));
        });
    }

    /// <summary>
    /// Parses the caller's Keycloak subject (<c>sub</c> claim) into a <see cref="Guid"/>.
    /// Returns <c>false</c> when the token has no subject or the value is not a valid GUID.
    /// </summary>
    private static bool TryGetCustomerId(ICurrentUser currentUser, out Guid customerId)
    {
        customerId = Guid.Empty;
        return currentUser.IsAuthenticated
            && currentUser.Sub is not null
            && Guid.TryParse(currentUser.Sub, out customerId);
    }
}

// Only ProductId and Quantity remain in the body — CustomerId is read from the token.
public sealed record AddToCartRequest(Guid ProductId, int Quantity);
public sealed record RemoveFromCartRequest(Guid ProductId);
public sealed record DecreaseCartItemRequest(Guid ProductId);
