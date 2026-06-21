using OrderSphere.Ordering.Domain.Enums;

namespace OrderSphere.Ordering.Domain.Entities;

/// <summary>
/// Queryable projection of the distributed checkout-to-payment flow, keyed by the
/// Service Bus correlation id. Written inline within the worker transactions that
/// already mutate the order, so it commits atomically with the order state change.
/// Not an <c>AuditableEntity</c>: it is a read-model with no soft-delete semantics
/// and manages its own timestamps.
/// </summary>
public class OrderSaga
{
    public Guid CorrelationId { get; private set; }
    public Guid? OrderId { get; private set; }
    public SagaState State { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? PaymentRequestedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    /// <summary>Last failure reason recorded on the saga (e.g. payment failure). Null on the happy path.</summary>
    public string? LastError { get; private set; }

    private OrderSaga() { }

    /// <summary>
    /// Creates the saga at the point the order is persisted. The OrderProcessor creates the
    /// order and queues the payment request in one transaction, so the saga starts at
    /// <see cref="SagaState.PaymentRequested"/> via <see cref="MarkPaymentRequested"/>.
    /// </summary>
    public static OrderSaga Start(Guid correlationId, Guid orderId)
    {
        var now = DateTime.UtcNow;
        return new OrderSaga
        {
            CorrelationId = correlationId,
            OrderId = orderId,
            State = SagaState.OrderCreated,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkPaymentRequested()
    {
        State = SagaState.PaymentRequested;
        PaymentRequestedAt = DateTime.UtcNow;
        Touch();
    }

    public void MarkConfirmed()
    {
        State = SagaState.Confirmed;
        CompletedAt = DateTime.UtcNow;
        Touch();
    }

    public void MarkCancelled(string? reason)
    {
        State = SagaState.Cancelled;
        LastError = Truncate(reason);
        CompletedAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    /// Payment succeeded but confirmation failed after bounded retries; a refund has been
    /// requested. Non-terminal: <see cref="MarkRefunded"/> closes the loop. <c>CompletedAt</c>
    /// is left null until the refund is confirmed.
    /// </summary>
    public void MarkCompensationPending(string? reason)
    {
        State = SagaState.CompensationPending;
        LastError = Truncate(reason);
        Touch();
    }

    /// <summary>Refund confirmed after a failed confirmation. Terminal.</summary>
    public void MarkRefunded()
    {
        State = SagaState.Refunded;
        CompletedAt = DateTime.UtcNow;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;

    private static string? Truncate(string? value)
        => value is { Length: > 1024 } ? value[..1024] : value;
}
