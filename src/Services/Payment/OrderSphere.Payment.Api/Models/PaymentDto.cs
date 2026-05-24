namespace OrderSphere.Payment.Api.Models;

public sealed record PaymentDto(
    Guid Id,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string Status,
    string? TransactionId,
    string? FailureReason,
    DateTime CreatedAt);
