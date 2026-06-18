using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.Errors;

namespace OrderSphere.Ordering.Domain.Entities;

/// <summary>
/// A discount coupon. Replaces the former hardcoded coupon table: persisted, with expiry,
/// usage limits and flat/percentage discounts. Codes are stored normalized (upper-case, trimmed).
/// </summary>
public class Coupon : AuditableEntity<CouponId>, IAggregateRoot
{
    public string Code { get; private set; }
    public DiscountType DiscountType { get; private set; }

    /// <summary>Flat amount in EUR, or a percentage (0–100) when <see cref="DiscountType"/> is Percentage.</summary>
    public decimal Value { get; private set; }
    public decimal? MinSubtotal { get; private set; }
    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidUntil { get; private set; }
    public int? MaxRedemptions { get; private set; }
    public int RedeemedCount { get; private set; }
    public bool IsActive { get; private set; }

    private Coupon() => Code = string.Empty; // EF

    public Coupon(
        string code,
        DiscountType discountType,
        decimal value,
        decimal? minSubtotal,
        DateTime? validFrom,
        DateTime? validUntil,
        int? maxRedemptions,
        bool isActive = true)
    {
        Id = CouponId.New();
        Code = Normalize(code);
        DiscountType = discountType;
        Value = value;
        MinSubtotal = minSubtotal;
        ValidFrom = validFrom;
        ValidUntil = validUntil;
        MaxRedemptions = maxRedemptions;
        RedeemedCount = 0;
        IsActive = isActive;
    }

    public void Update(
        DiscountType discountType,
        decimal value,
        decimal? minSubtotal,
        DateTime? validFrom,
        DateTime? validUntil,
        int? maxRedemptions,
        bool isActive)
    {
        DiscountType = discountType;
        Value = value;
        MinSubtotal = minSubtotal;
        ValidFrom = validFrom;
        ValidUntil = validUntil;
        MaxRedemptions = maxRedemptions;
        IsActive = isActive;
    }

    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Validates the coupon for a given subtotal at a point in time and returns the discount
    /// amount, capped at the subtotal. Pure — does not mutate redemption state.
    /// </summary>
    public Result<decimal> CalculateDiscount(decimal subtotal, DateTime nowUtc)
    {
        if (!IsActive)
            return Result<decimal>.Failure(CouponErrors.NotActive);
        if (ValidFrom is { } from && nowUtc < from)
            return Result<decimal>.Failure(CouponErrors.NotYetValid);
        if (ValidUntil is { } until && nowUtc > until)
            return Result<decimal>.Failure(CouponErrors.Expired);
        if (MaxRedemptions is { } max && RedeemedCount >= max)
            return Result<decimal>.Failure(CouponErrors.UsageLimitReached);
        if (MinSubtotal is { } min && subtotal < min)
            return Result<decimal>.Failure(CouponErrors.MinSubtotalNotMet);

        var discount = DiscountType == DiscountType.Percentage
            ? Math.Round(subtotal * (Value / 100m), 2, MidpointRounding.AwayFromZero)
            : Value;

        return Result<decimal>.Success(Math.Min(discount, subtotal));
    }

    /// <summary>Records a single redemption. Call inside the checkout transaction after a
    /// successful <see cref="CalculateDiscount"/> (WS-2b).</summary>
    public Result Redeem()
    {
        if (MaxRedemptions is { } max && RedeemedCount >= max)
            return Result.Failure(CouponErrors.UsageLimitReached);

        RedeemedCount++;
        return Result.Success();
    }

    public static string Normalize(string code) => code.Trim().ToUpperInvariant();
}
