namespace OrderSphere.Payment.Infrastructure.Providers;

public interface IPaymentProviderFactory
{
    IPaymentProvider? GetProvider(string paymentMethod);
}
