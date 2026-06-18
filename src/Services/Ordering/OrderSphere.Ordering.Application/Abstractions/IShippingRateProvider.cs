namespace OrderSphere.Ordering.Application.Abstractions;

/// <summary>
/// Calculates the shipping cost for an order subtotal. Invoked server-side during order
/// processing so the charged amount is authoritative (the client only shows an estimate).
/// </summary>
public interface IShippingRateProvider
{
    decimal Calculate(decimal subtotal);
}
