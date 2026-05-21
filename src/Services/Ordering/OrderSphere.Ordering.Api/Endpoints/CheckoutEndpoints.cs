using MediatR;
using OrderSphere.Ordering.Api.Features.Checkout;
using OrderSphere.Ordering.Api.Models;

namespace OrderSphere.Ordering.Api.Endpoints;

public static class CheckoutEndpoints
{
    public static void MapCheckoutEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/checkout", async (CheckoutRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CheckoutCartCommand(request), ct);
            return result.IsSuccess
                ? Results.Ok(new { CorrelationId = result.Value })
                : Results.BadRequest(new ErrorResponse(result.Error.Code, result.Error.Description));
        }).RequireAuthorization();
    }
}
