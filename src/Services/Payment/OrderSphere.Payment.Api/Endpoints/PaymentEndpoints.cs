using MediatR;
using OrderSphere.Payment.Application.Features.Payments;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Payment.Api.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/payments")
            .RequireAuthorization();

        group.MapGet("/by-order/{orderId:guid}", async (
            Guid orderId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPaymentByOrderIdQuery(orderId), ct);
            return result.ToHttpResult();
        });

        group.MapGet("/{paymentId:guid}", async (
            Guid paymentId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPaymentByIdQuery(paymentId), ct);
            return result.ToHttpResult();
        });
    }
}
