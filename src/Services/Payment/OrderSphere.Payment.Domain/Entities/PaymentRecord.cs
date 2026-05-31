using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Payment.Domain.DomainEvents;
using OrderSphere.Payment.Domain.Enums;

namespace OrderSphere.Payment.Domain.Entities;

public class PaymentRecord : AuditableEntity<PaymentId>, IAggregateRoot
{
    public OrderId OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "EUR";
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
        Amount = amount;
        Currency = currency;
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
}
