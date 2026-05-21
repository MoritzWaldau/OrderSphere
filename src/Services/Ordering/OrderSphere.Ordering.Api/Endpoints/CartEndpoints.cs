using MediatR;
using OrderSphere.Ordering.Api.Features.Cart;
using OrderSphere.Ordering.Api.Models;

namespace OrderSphere.Ordering.Api.Endpoints;

public static class CartEndpoints
{
    public static void MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/cart").RequireAuthorization();

        group.MapGet("/{customerId:guid}", async (Guid customerId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetCartQuery(customerId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new ErrorResponse(result.Error.Code, result.Error.Description));
        });

        group.MapPost("/add", async (AddToCartRequest req, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new AddToCartCommand(req.CustomerId, req.ProductId, req.Quantity), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new ErrorResponse(result.Error.Code, result.Error.Description));
        });

        group.MapDelete("/remove", async (RemoveFromCartRequest req, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RemoveFromCartCommand(req.CustomerId, req.ProductId), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new ErrorResponse(result.Error.Code, result.Error.Description));
        });

        group.MapPut("/decrease", async (DecreaseCartItemRequest req, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DecreaseCartItemQuantityCommand(req.CustomerId, req.ProductId), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new ErrorResponse(result.Error.Code, result.Error.Description));
        });
    }
}

public sealed record AddToCartRequest(Guid CustomerId, Guid ProductId, int Quantity);
public sealed record RemoveFromCartRequest(Guid CustomerId, Guid ProductId);
public sealed record DecreaseCartItemRequest(Guid CustomerId, Guid ProductId);
