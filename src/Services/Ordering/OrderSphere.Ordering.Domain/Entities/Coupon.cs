using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Domain.ValueObjects;

namespace OrderSphere.Ordering.Domain.Entities;

/// <summary>
/// A discount coupon. Supports flat, percentage, and tiered discounts; optionally scoped to a
/// set of product categories. Codes are stored normalized (upper-case, trimmed).
/// </summary>
public class Coupon : AuditableEntity<CouponId>, IAggregateRoot
{
    public string Code { get; private set; }
    public DiscountType DiscountType { get; private set; }

    /// <summary>Flat amount in EUR, or a percentage (0–100) when <see cref="DiscountType"/> is Percentage.
    /// Not used for <see cref="DiscountType.Tiered"/> — discount value is carried per tier.</summary>
    public decimal Value { get; private set; }
    public decimal? MinSubtotal { get; private set; }
    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidUntil { get; private set; }
    public int? MaxRedemptions { get; private set; }
    public int RedeemedCount { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<CouponTier> _tiers = [];

    /// <summary>Ordered thresholds for <see cref="DiscountType.Tiered"/> coupons.
    /// Empty for Flat and Percentage types.</summary>
    public IReadOnlyList<CouponTier> Tiers => _tiers;

    /// <summary>When non-empty, the discount applies only to items belonging to one of these
    /// category IDs. An empty set means all categories qualify.</summary>
    public List<Guid> ScopedCategoryIds { get; private set; } = [];

    private Coupon() => Code = string.Empty; // EF

    public Coupon(
        string code,
        DiscountType discountType,
        decimal value,
        decimal? minSubtotal,
        DateTime? validFrom,
        DateTime? validUntil,
        int? maxRedemptions,
        bool isActive = true,
        IEnumerable<CouponTier>? tiers = null,
        IEnumerable<Guid>? scopedCategoryIds = null)
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
        if (tiers is not null) _tiers.AddRange(tiers);
        if (scopedCategoryIds is not null) ScopedCategoryIds = [.. scopedCategoryIds];
    }

    public void Update(
        DiscountType discountType,
        decimal value,
        decimal? minSubtotal,
        DateTime? validFrom,
        DateTime? validUntil,
        int? maxRedemptions,
        bool isActive,
        IEnumerable<CouponTier>? tiers = null,
        IEnumerable<Guid>? scopedCategoryIds = null)
    {
        DiscountType = discountType;
        Value = value;
        MinSubtotal = minSubtotal;
        ValidFrom = validFrom;
        ValidUntil = validUntil;
        MaxRedemptions = maxRedemptions;
        IsActive = isActive;
        _tiers.Clear();
        if (tiers is not null) _tiers.AddRange(tiers);
        ScopedCategoryIds = scopedCategoryIds is not null ? [.. scopedCategoryIds] : [];
    }

    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Computes the subtotal relevant for this coupon from a list of order line items.
    /// When <see cref="ScopedCategoryIds"/> is non-empty only lines whose category is in the
    /// scope set contribute; otherwise the full subtotal is returned.
    /// </summary>
    public decimal ComputeScopedSubtotal(IEnumerable<(Guid? CategoryId, decimal LineTotal)> items)
    {
        if (ScopedCategoryIds.Count == 0)
            return items.Sum(i => i.LineTotal);

        return items
            .Where(i => i.CategoryId.HasValue && ScopedCategoryIds.Contains(i.CategoryId.Value))
            .Sum(i => i.LineTotal);
    }

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

        decimal discount;

        if (DiscountType == DiscountType.Tiered)
        {
            var tier = _tiers
                .Where(t => subtotal >= t.MinSubtotal)
                .MaxBy(t => t.MinSubtotal);

            if (tier is null)
                return Result<decimal>.Failure(CouponErrors.MinSubtotalNotMet);

            discount = tier.DiscountValue;
        }
        else
        {
            if (MinSubtotal is { } min && subtotal < min)
                return Result<decimal>.Failure(CouponErrors.MinSubtotalNotMet);

            discount = DiscountType == DiscountType.Percentage
                ? Math.Round(subtotal * (Value / 100m), 2, MidpointRounding.AwayFromZero)
                : Value;
        }

        return Result<decimal>.Success(Math.Min(discount, subtotal));
    }

    /// <summary>Records a single redemption. Call inside the checkout transaction after a
    /// successful <see cref="CalculateDiscount"/>.</summary>
    public Result Redeem()
    {
        if (MaxRedemptions is { } max && RedeemedCount >= max)
            return Result.Failure(CouponErrors.UsageLimitReached);

        RedeemedCount++;
        return Result.Success();
    }

    public static string Normalize(string code) => code.Trim().ToUpperInvariant();
}
