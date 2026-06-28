namespace OrderSphere.Ordering.Domain.Enums;

public enum DiscountType
{
    /// <summary>A fixed amount in the order currency (EUR).</summary>
    Flat = 0,

    /// <summary>A percentage of the subtotal (0–100).</summary>
    Percentage = 1,

    /// <summary>Graduated thresholds: the highest qualifying <see cref="OrderSphere.Ordering.Domain.ValueObjects.CouponTier"/> determines the flat discount.</summary>
    Tiered = 2,
}
