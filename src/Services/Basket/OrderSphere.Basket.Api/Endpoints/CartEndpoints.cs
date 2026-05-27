using MediatR;
using OrderSphere.Basket.Api.Features.Cart;
using OrderSphere.Basket.Api.Models;
using OrderSphere.BuildingBlocks.Security;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Basket.Api.Endpoints;

public static class CartEndpoints
{
    public static void MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/cart").RequireAuthorization();

        group.MapGet("/", async (ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetCustomerId(currentUser, out var customerId))
                return Results.Unauthorized();

            var result = await mediator.Send(new GetCartQuery(customerId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new ErrorResponse(result.Error.Code, result.Error.Description));
        });

        group.MapPost("/add", async (AddToCartRequest req, ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetCustomerId(currentUser, out var customerId))
                return Results.Unauthorized();

            var result = await mediator.Send(
                new AddToCartCommand(customerId, ProductId.From(req.ProductId), req.Quantity), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new ErrorResponse(result.Error.Code, result.Error.Description));
        });

        group.MapDelete("/remove", async (Guid productId, ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetCustomerId(currentUser, out var customerId))
                return Results.Unauthorized();

            var result = await mediator.Send(
                new RemoveFromCartCommand(customerId, ProductId.From(productId)), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new ErrorResponse(result.Error.Code, result.Error.Description));
        });

        group.MapPut("/decrease", async (DecreaseCartItemRequest req, ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
        {
            if (!TryGetCustomerId(currentUser, out var customerId))
                return Results.Unauthorized();

            var result = await mediator.Send(
                new DecreaseCartItemQuantityCommand(customerId, ProductId.From(req.ProductId)), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new ErrorResponse(result.Error.Code, result.Error.Description));
        });
    }

    /// <summary>
    /// Parses the Keycloak subject claim into a <see cref="CustomerId"/>.
    /// The sub is a UUID that serves as the customer identifier across services.
    /// </summary>
    private static bool TryGetCustomerId(ICurrentUser currentUser, out CustomerId customerId)
    {
        customerId = CustomerId.Empty;
        if (!currentUser.IsAuthenticated || currentUser.Sub is null)
            return false;

        if (!Guid.TryParse(currentUser.Sub, out var guid))
            return false;

        customerId = CustomerId.From(guid);
        return true;
    }
}

public sealed record AddToCartRequest(Guid ProductId, int Quantity);
public sealed record DecreaseCartItemRequest(Guid ProductId);
