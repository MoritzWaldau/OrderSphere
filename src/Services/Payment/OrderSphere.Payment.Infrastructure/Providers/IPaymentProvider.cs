using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Payment.Infrastructure.Providers;

public interface IPaymentProvider
{
    string MethodName { get; }
    Task<Result<PaymentProviderResult>> AuthorizeAsync(PaymentRequest request, CancellationToken ct = default);
    Task<Result<PaymentProviderResult>> CaptureAsync(string transactionId, decimal amount, CancellationToken ct = default);
    Task<Result> RefundAsync(string transactionId, decimal amount, CancellationToken ct = default);
}

public sealed record PaymentRequest(
    Guid OrderId,
    decimal Amount,
    string Currency,
    string CustomerEmail);

public sealed record PaymentProviderResult(string TransactionId);
