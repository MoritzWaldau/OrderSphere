using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Payment.Api.Models;
using OrderSphere.Payment.Infrastructure.Persistence;

namespace OrderSphere.Payment.Api.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/payments")
            .RequireAuthorization();

        group.MapGet("/by-order/{orderId:guid}", async (
            Guid orderId,
            PaymentDbContext db,
            CancellationToken ct) =>
        {
            var payment = await db.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.OrderId == OrderId.From(orderId), ct);

            return payment is null
                ? Results.NotFound()
                : Results.Ok(new PaymentDto(
                    payment.Id.Value,
                    payment.OrderId.Value,
                    payment.Amount,
                    payment.Currency,
                    payment.PaymentMethod,
                    payment.Status.ToString(),
                    payment.TransactionId,
                    payment.FailureReason,
                    payment.CreatedAt));
        });

        group.MapGet("/{paymentId:guid}", async (
            Guid paymentId,
            PaymentDbContext db,
            CancellationToken ct) =>
        {
            var payment = await db.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == PaymentId.From(paymentId), ct);

            return payment is null
                ? Results.NotFound()
                : Results.Ok(new PaymentDto(
                    payment.Id.Value,
                    payment.OrderId.Value,
                    payment.Amount,
                    payment.Currency,
                    payment.PaymentMethod,
                    payment.Status.ToString(),
                    payment.TransactionId,
                    payment.FailureReason,
                    payment.CreatedAt));
        });
    }
}
