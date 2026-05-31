namespace OrderSphere.Payment.Domain.Enums;

public enum PaymentStatus
{
    Pending,
    Authorized,
    Captured,
    Failed,
    Refunded
}
