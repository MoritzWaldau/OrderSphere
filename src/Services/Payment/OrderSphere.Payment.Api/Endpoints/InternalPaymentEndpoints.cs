using Microsoft.EntityFrameworkCore;
using OrderSphere.Payment.Api.Models;
using OrderSphere.Payment.Infrastructure.Persistence;

namespace OrderSphere.Payment.Api.Endpoints;

public static class InternalPaymentEndpoints
{
    public static void MapInternalPaymentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/internal/payments");

        group.MapGet("/by-order/{orderId:guid}", async (
            Guid orderId,
            PaymentDbContext db,
            CancellationToken ct) =>
        {
            var payment = await db.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

            return payment is null
                ? Results.NotFound()
                : Results.Ok(new PaymentDto(
                    payment.Id,
                    payment.OrderId,
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
