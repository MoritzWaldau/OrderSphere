using Microsoft.Extensions.Configuration;
using OrderSphere.Ordering.Application.Abstractions;

namespace OrderSphere.Ordering.Infrastructure.Shipping;

/// <summary>
/// Flat shipping rate that is waived once the subtotal reaches a free-shipping threshold.
/// Configured via <c>Shipping:FlatRate</c> (default 4.99) and <c>Shipping:FreeThreshold</c> (default 50).
/// </summary>
public sealed class FlatRateShippingProvider : IShippingRateProvider
{
    private readonly decimal _flatRate;
    private readonly decimal _freeThreshold;

    public FlatRateShippingProvider(IConfiguration configuration)
    {
        _flatRate = configuration.GetValue<decimal?>("Shipping:FlatRate") ?? 4.99m;
        _freeThreshold = configuration.GetValue<decimal?>("Shipping:FreeThreshold") ?? 50m;
    }

    public decimal Calculate(decimal subtotal) => subtotal >= _freeThreshold ? 0m : _flatRate;
}
