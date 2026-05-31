using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Payment.Application.Abstractions;
using OrderSphere.Payment.Application.Models;
using OrderSphere.Payment.Domain.Entities;
using OrderSphere.Payment.Domain.Errors;

namespace OrderSphere.Payment.Application.Features.Payments;

/// <summary>
/// Loads the payment record associated with an order. Used by the public customer-facing
/// endpoint and the internal service-to-service endpoint.
/// </summary>
public sealed record GetPaymentByOrderIdQuery(Guid OrderId) : IQuery<Result<PaymentDto>>;

public sealed class GetPaymentByOrderIdQueryHandler(
    IPaymentDbContext context,
    ILogger<GetPaymentByOrderIdQueryHandler> logger
) : IQueryHandler<GetPaymentByOrderIdQuery, Result<PaymentDto>>
{
    public async Task<Result<PaymentDto>> Handle(GetPaymentByOrderIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.OrderId == OrderId.From(request.OrderId), cancellationToken);

            return payment is null
                ? Result<PaymentDto>.Failure(PaymentErrors.PaymentNotFound)
                : Result<PaymentDto>.Success(payment.ToDto());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving payment for order {OrderId}", request.OrderId);
            return Result<PaymentDto>.Failure(PaymentErrors.PaymentNotFound);
        }
    }
}

internal static class PaymentRecordMappings
{
    public static PaymentDto ToDto(this PaymentRecord payment) => new(
        payment.Id.Value,
        payment.OrderId.Value,
        payment.Amount,
        payment.Currency,
        payment.PaymentMethod,
        payment.Status.ToString(),
        payment.TransactionId,
        payment.FailureReason,
        payment.CreatedAt);
}
