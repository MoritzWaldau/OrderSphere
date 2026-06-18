namespace OrderSphere.Web.Services;

/// <summary>
/// Client-side shipping estimate shown in the checkout summary. Mirrors the server-side
/// flat-rate-with-free-threshold rule (Ordering's FlatRateShippingProvider); the charged
/// amount remains the server's authoritative calculation.
/// </summary>
public static class ShippingEstimate
{
    public const decimal FlatRate = 4.99m;
    public const decimal FreeThreshold = 50m;

    public static decimal For(decimal subtotal) => subtotal >= FreeThreshold ? 0m : FlatRate;
}
