using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.BuildingBlocks.ValueObjects;
using OrderSphere.Payment.Domain.DomainEvents;
using OrderSphere.Payment.Domain.Enums;

namespace OrderSphere.Payment.Domain.Entities;

public class PaymentRecord : AuditableEntity<PaymentId>, IAggregateRoot
{
    public OrderId OrderId { get; private set; }

    /// <summary>The charged amount and its currency as a single value object.</summary>
    public Money Amount { get; private set; } = null!;
    public string PaymentMethod { get; private set; } = "";
    public string CustomerEmail { get; private set; } = "";
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string? TransactionId { get; private set; }
    public string? FailureReason { get; private set; }
    public Guid CorrelationId { get; private set; }

    private PaymentRecord()
    {
        OrderId = OrderId.Empty;
    }

    public PaymentRecord(
        OrderId orderId,
        decimal amount,
        string currency,
        string paymentMethod,
        string customerEmail,
        Guid correlationId)
    {
        Id = PaymentId.New();
        OrderId = orderId;
        Amount = Money.Of(amount, currency);
        PaymentMethod = paymentMethod;
        CustomerEmail = customerEmail;
        CorrelationId = correlationId;
        Status = PaymentStatus.Pending;
    }

    public void MarkAuthorized(string transactionId)
    {
        TransactionId = transactionId;
        Status = PaymentStatus.Authorized;
        RaiseDomainEvent(new PaymentAuthorizedDomainEvent(Id, OrderId, transactionId));
    }

    public void MarkCaptured(string transactionId)
    {
        TransactionId = transactionId;
        Status = PaymentStatus.Captured;
        RaiseDomainEvent(new PaymentCapturedDomainEvent(Id, OrderId, transactionId));
    }

    public void MarkFailed(string reason)
    {
        FailureReason = reason;
        Status = PaymentStatus.Failed;
        RaiseDomainEvent(new PaymentFailedDomainEvent(Id, OrderId, reason));
    }

    public void MarkRefunded()
    {
        Status = PaymentStatus.Refunded;
    }

    /// <summary>GDPR right-to-erasure: overwrites the customer email, keeping the payment
    /// record (amount, status, transaction id) for financial retention.</summary>
    public void AnonymizeCustomerEmail()
    {
        CustomerEmail = $"erased-{Id.Value}@erased.invalid";
    }
}
