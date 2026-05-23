using MediatR;
using OrderSphere.BuildingBlocks.Security;
using OrderSphere.Ordering.Api.Features.Checkout;
using OrderSphere.Ordering.Api.Models;

namespace OrderSphere.Ordering.Api.Endpoints;

public static class CheckoutEndpoints
{
    public static void MapCheckoutEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/checkout",
            async (CheckoutRequest request, ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
            {
                if (!currentUser.IsAuthenticated || currentUser.Sub is null
                    || !Guid.TryParse(currentUser.Sub, out var customerId))
                    return Results.Unauthorized();

                var command = new CheckoutCartCommand(
                    customerId,
                    currentUser.Email ?? string.Empty,
                    currentUser.Name ?? string.Empty,
                    request.ShippingAddress,
                    request.PaymentMethod);

                var result = await mediator.Send(command, ct);
                return result.IsSuccess
                    ? Results.Ok(new { CorrelationId = result.Value })
                    : Results.BadRequest(new ErrorResponse(result.Error.Code, result.Error.Description));
            }).RequireAuthorization();
    }
}
