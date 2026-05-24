using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.Payment.Domain.Enums;

namespace OrderSphere.Payment.Domain.Entities;

public class PaymentRecord : AuditableEntity
{
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public string PaymentMethod { get; private set; } = "";
    public string CustomerEmail { get; private set; } = "";
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string? TransactionId { get; private set; }
    public string? FailureReason { get; private set; }
    public Guid CorrelationId { get; private set; }

    private PaymentRecord() { }

    public PaymentRecord(
        Guid orderId,
        decimal amount,
        string currency,
        string paymentMethod,
        string customerEmail,
        Guid correlationId)
    {
        Id = Guid.NewGuid();
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
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCaptured(string transactionId)
    {
        TransactionId = transactionId;
        Status = PaymentStatus.Captured;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        FailureReason = reason;
        Status = PaymentStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkRefunded()
    {
        Status = PaymentStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
    }
}
