using MediatR;
using OrderSphere.Payment.Application.Features.Payments;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Payment.Api.Endpoints;

public static class InternalPaymentEndpoints
{
    public static void MapInternalPaymentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/internal/payments");

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
