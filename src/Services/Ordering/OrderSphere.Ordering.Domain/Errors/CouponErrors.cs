using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Ordering.Domain.Errors;

public static class CouponErrors
{
    public static readonly Error CodeRequired =
        new("Coupon.CodeRequired", "Bitte gib einen Gutscheincode ein.", ErrorType.Failure);

    public static readonly Error InvalidCode =
        new("Coupon.InvalidCode", "Dieser Gutscheincode ist ungültig.", ErrorType.Failure);

    public static readonly Error NotActive =
        new("Coupon.NotActive", "Dieser Gutscheincode ist nicht aktiv.", ErrorType.Failure);

    public static readonly Error NotYetValid =
        new("Coupon.NotYetValid", "Dieser Gutscheincode ist noch nicht gültig.", ErrorType.Failure);

    public static readonly Error Expired =
        new("Coupon.Expired", "Dieser Gutscheincode ist abgelaufen.", ErrorType.Failure);

    public static readonly Error UsageLimitReached =
        new("Coupon.UsageLimitReached", "Dieser Gutscheincode wurde bereits zu oft eingelöst.", ErrorType.Failure);

    public static readonly Error MinSubtotalNotMet =
        new("Coupon.MinSubtotalNotMet", "Der Mindestbestellwert für diesen Gutschein ist nicht erreicht.", ErrorType.Failure);

    public static readonly Error NotFound =
        new("Coupon.NotFound", "Gutschein nicht gefunden.", ErrorType.NotFound);

    public static readonly Error CodeExists =
        new("Coupon.CodeExists", "Ein Gutschein mit diesem Code existiert bereits.", ErrorType.Conflict);
}
