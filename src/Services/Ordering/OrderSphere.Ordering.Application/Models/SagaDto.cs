namespace OrderSphere.Ordering.Application.Models;

/// <summary>
/// Read-model view of the checkout-to-payment saga for a single correlation id.
/// </summary>
public sealed record SagaDto(
    Guid CorrelationId,
    Guid? OrderId,
    string State,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? PaymentRequestedAt,
    DateTime? CompletedAt,
    string? LastError);
