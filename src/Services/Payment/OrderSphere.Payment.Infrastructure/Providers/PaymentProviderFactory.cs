namespace OrderSphere.Payment.Infrastructure.Providers;

internal sealed class PaymentProviderFactory(IEnumerable<IPaymentProvider> providers) : IPaymentProviderFactory
{
    private readonly Dictionary<string, IPaymentProvider> _providers =
        providers.ToDictionary(p => p.MethodName, StringComparer.OrdinalIgnoreCase);

    public IPaymentProvider? GetProvider(string paymentMethod)
    {
        _providers.TryGetValue(paymentMethod, out var provider);
        return provider;
    }
}
