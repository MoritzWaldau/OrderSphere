using OrderSphere.Domain.Primitives;

namespace OrderSphere.Domain.Errors;

public static class CouponErrors
{
    public static readonly Error CodeRequired =
        new("Coupon.CodeRequired", "A coupon code is required.");

    public static readonly Error InvalidCode =
        new("Coupon.InvalidCode", "The coupon code is invalid.");
}
