using MediatR;
using OrderSphere.Payment.Application.Features.Payments;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Payment.Api.Endpoints;

public static class InternalPaymentEndpoints
{
    public static void MapInternalPaymentEndpoints(this WebApplication app)
    {
        // D4 — requires a valid client-credentials token (any authenticated caller); M2M
        // tokens carry no role claims, so no role-based policy is applied here.
        var group = app.MapGroup("/internal/payments").RequireAuthorization();

        group.MapGet("/by-order/{orderId:guid}", async (
            Guid orderId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPaymentByOrderIdQuery(orderId), ct);
            return result.ToHttpResult();
        });
    }
}
