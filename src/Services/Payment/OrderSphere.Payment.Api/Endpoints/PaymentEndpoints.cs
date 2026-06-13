using Asp.Versioning;
using MediatR;
using OrderSphere.Payment.Application.Features.Payments;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Payment.Api.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("api/v{version:apiVersion}/payments")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(1.0)
            .MapToApiVersion(1.0)
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
