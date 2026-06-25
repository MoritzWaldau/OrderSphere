using OrderSphere.Payment.Domain.Entities;

namespace OrderSphere.Payment.Application.Models;

internal static class PaymentRecordMappings
{
    public static PaymentDto ToDto(this PaymentRecord payment) => new(
        payment.Id.Value,
        payment.OrderId.Value,
        payment.Amount,
        payment.Amount.Currency,
        payment.PaymentMethod,
        payment.Status.ToString(),
        payment.TransactionId,
        payment.FailureReason,
        payment.CreatedAt);
}
