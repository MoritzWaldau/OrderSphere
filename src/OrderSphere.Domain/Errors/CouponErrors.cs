using OrderSphere.Domain.Primitives;

namespace OrderSphere.Domain.Errors;

public static class CouponErrors
{
    public static readonly Error CodeRequired =
        new("Coupon.CodeRequired", "Bitte gib einen Gutscheincode ein.");

    public static readonly Error InvalidCode =
        new("Coupon.InvalidCode", "Dieser Gutscheincode ist ungültig.");
}
