using MediatR;
using OrderSphere.BuildingBlocks.Security;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Api.Features.Checkout;
using OrderSphere.Ordering.Api.Models;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Ordering.Api.Endpoints;

public static class CheckoutEndpoints
{
    public static void MapCheckoutEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/checkout",
            async (CheckoutRequest request, ICurrentUser currentUser, IMediator mediator,
                   HttpContext http, CancellationToken ct) =>
            {
                if (!currentUser.IsAuthenticated || currentUser.Sub is null
                    || !Guid.TryParse(currentUser.Sub, out var customerId))
                    return Results.Unauthorized();

                // Callers SHOULD send a stable Idempotency-Key (UUID v4/v7) per checkout attempt.
                // If omitted, a new key is generated — retries without the header are not deduplicated.
                var idempotencyKey = http.Request.Headers.TryGetValue("Idempotency-Key", out var raw)
                    && Guid.TryParse(raw.ToString(), out var parsedKey)
                    ? parsedKey
                    : Guid.CreateVersion7();

                var command = new CheckoutCartCommand(
                    CustomerId.From(customerId),
                    currentUser.Email ?? string.Empty,
                    currentUser.Name ?? string.Empty,
                    request.ShippingAddress,
                    request.PaymentMethod,
                    idempotencyKey);

                var result = await mediator.Send(command, ct);
                return result.ToHttpResult(correlationId => Results.Ok(new { CorrelationId = correlationId }));
            }).RequireAuthorization();
    }
}
