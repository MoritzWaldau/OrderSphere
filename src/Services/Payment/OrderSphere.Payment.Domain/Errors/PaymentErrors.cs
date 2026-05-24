using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Payment.Domain.Errors;

public static class PaymentErrors
{
    public static readonly Error PaymentNotFound = new("Payment.NotFound", "Payment record not found.");
    public static readonly Error UnsupportedMethod = new("Payment.UnsupportedMethod", "Payment method is not supported.");
    public static readonly Error AuthorizationFailed = new("Payment.AuthorizationFailed", "Payment authorization failed.");
    public static readonly Error CaptureFailed = new("Payment.CaptureFailed", "Payment capture failed.");
    public static readonly Error RefundFailed = new("Payment.RefundFailed", "Payment refund failed.");
    public static readonly Error DuplicatePayment = new("Payment.Duplicate", "A payment for this order already exists.");
}
