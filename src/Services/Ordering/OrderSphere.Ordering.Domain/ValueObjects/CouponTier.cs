namespace OrderSphere.Ordering.Domain.ValueObjects;

/// <summary>
/// One threshold level of a tiered coupon. The highest tier whose
/// <see cref="MinSubtotal"/> is met by the (scoped) order subtotal is applied.
/// </summary>
public sealed record CouponTier(decimal MinSubtotal, decimal DiscountValue);
