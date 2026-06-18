namespace OrderSphere.Ordering.Domain.Enums;

public enum DiscountType
{
    /// <summary>A fixed amount in the order currency (EUR).</summary>
    Flat = 0,

    /// <summary>A percentage of the subtotal (0–100).</summary>
    Percentage = 1,
}
