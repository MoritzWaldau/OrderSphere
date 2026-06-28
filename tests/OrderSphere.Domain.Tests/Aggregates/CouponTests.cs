using FluentAssertions;
using OrderSphere.Ordering.Domain.Entities;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Domain.ValueObjects;
using Xunit;

namespace OrderSphere.Domain.Tests.Aggregates;

public sealed class CouponTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Coupon Flat(decimal value, decimal? min = null, bool active = true) =>
        new("SAVE", DiscountType.Flat, value, min, null, null, null, active);

    private static Coupon Tiered(params (decimal Min, decimal Discount)[] tiers) =>
        new("TIERED", DiscountType.Tiered, 0m, null, null, null, null, tiers: tiers.Select(t => new CouponTier(t.Min, t.Discount)));


    [Fact]
    public void Constructor_NormalizesCodeToUpperCase()
    {
        var coupon = new Coupon("welcome10", DiscountType.Flat, 10m, null, null, null, null);
        coupon.Code.Should().Be("WELCOME10");
        coupon.RedeemedCount.Should().Be(0);
    }


    [Fact]
    public void CalculateDiscount_Flat_ReturnsFixedAmount()
    {
        var result = Flat(10m).CalculateDiscount(50m, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(10m);
    }

    [Fact]
    public void CalculateDiscount_Flat_CapsAtSubtotal()
    {
        var result = Flat(10m).CalculateDiscount(8m, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(8m);
    }


    [Fact]
    public void CalculateDiscount_Percentage_ComputesAndRounds()
    {
        var coupon = new Coupon("P", DiscountType.Percentage, 10m, null, null, null, null);

        var result = coupon.CalculateDiscount(199.99m, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(20.00m); // 10% of 199.99 = 19.999 → 20.00
    }


    [Fact]
    public void CalculateDiscount_BelowMinSubtotal_Fails()
    {
        var result = Flat(15m, min: 100m).CalculateDiscount(80m, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.MinSubtotalNotMet);
    }

    [Fact]
    public void CalculateDiscount_Inactive_Fails()
    {
        var result = Flat(10m, active: false).CalculateDiscount(50m, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.NotActive);
    }

    [Fact]
    public void CalculateDiscount_Expired_Fails()
    {
        var coupon = new Coupon("E", DiscountType.Flat, 10m, null, null, Now.AddDays(-1), null);

        var result = coupon.CalculateDiscount(50m, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.Expired);
    }

    [Fact]
    public void CalculateDiscount_NotYetValid_Fails()
    {
        var coupon = new Coupon("F", DiscountType.Flat, 10m, null, Now.AddDays(1), null, null);

        var result = coupon.CalculateDiscount(50m, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.NotYetValid);
    }

    [Fact]
    public void CalculateDiscount_UsageLimitReached_Fails()
    {
        var coupon = new Coupon("L", DiscountType.Flat, 10m, null, null, null, maxRedemptions: 1);
        coupon.Redeem(); // RedeemedCount → 1, equals the limit

        var result = coupon.CalculateDiscount(50m, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.UsageLimitReached);
    }


    [Fact]
    public void Redeem_IncrementsCount_UntilLimit()
    {
        var coupon = new Coupon("R", DiscountType.Flat, 10m, null, null, null, maxRedemptions: 1);

        coupon.Redeem().IsSuccess.Should().BeTrue();
        coupon.RedeemedCount.Should().Be(1);

        var second = coupon.Redeem();
        second.IsFailure.Should().BeTrue();
        second.Error.Should().Be(CouponErrors.UsageLimitReached);
    }


    // --- Tiered discount tests ---

    [Fact]
    public void CalculateDiscount_Tiered_PicksHighestQualifyingTier()
    {
        // Subtotal 150 → qualifies for tier at 100 (€10) and tier at 150 (€20), picks €20.
        var coupon = Tiered((100m, 10m), (150m, 20m), (200m, 30m));

        var result = coupon.CalculateDiscount(150m, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(20m);
    }

    [Fact]
    public void CalculateDiscount_Tiered_OnlyLowestTierQualifies()
    {
        var coupon = Tiered((50m, 5m), (100m, 15m));

        var result = coupon.CalculateDiscount(75m, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(5m);
    }

    [Fact]
    public void CalculateDiscount_Tiered_NoTierQualifies_ReturnsMinSubtotalNotMet()
    {
        var coupon = Tiered((100m, 10m), (200m, 25m));

        var result = coupon.CalculateDiscount(50m, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.MinSubtotalNotMet);
    }

    [Fact]
    public void CalculateDiscount_Tiered_DiscountCapsAtSubtotal()
    {
        // Tier discount of €30 on a subtotal of €20 is capped to €20.
        var coupon = Tiered((0m, 30m));

        var result = coupon.CalculateDiscount(20m, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(20m);
    }


    // --- Category scoping tests ---

    private static Guid Cat1 = new("11111111-0000-0000-0000-000000000000");
    private static Guid Cat2 = new("22222222-0000-0000-0000-000000000000");
    private static Guid CatOther = new("33333333-0000-0000-0000-000000000000");

    private static Coupon ScopedFlat(decimal value, params Guid[] categoryIds) =>
        new("SCOPED", DiscountType.Flat, value, null, null, null, null, scopedCategoryIds: categoryIds);

    [Fact]
    public void ComputeScopedSubtotal_NoScopedCategories_ReturnsFull()
    {
        var coupon = Flat(10m); // no scope
        var items = new (Guid? CategoryId, decimal LineTotal)[]
        {
            (Cat1, 40m),
            (Cat2, 60m),
        };

        coupon.ComputeScopedSubtotal(items).Should().Be(100m);
    }

    [Fact]
    public void ComputeScopedSubtotal_WithScope_FiltersToMatchingCategories()
    {
        var coupon = ScopedFlat(10m, Cat1);
        var items = new (Guid? CategoryId, decimal LineTotal)[]
        {
            (Cat1, 40m),
            (Cat2, 60m),
            (CatOther, 20m),
        };

        coupon.ComputeScopedSubtotal(items).Should().Be(40m);
    }

    [Fact]
    public void ComputeScopedSubtotal_NoMatchingCategories_ReturnsZero()
    {
        var coupon = ScopedFlat(10m, Cat1);
        var items = new (Guid? CategoryId, decimal LineTotal)[]
        {
            (Cat2, 50m),
            (null, 30m), // item without category
        };

        coupon.ComputeScopedSubtotal(items).Should().Be(0m);
    }

    [Fact]
    public void CalculateDiscount_ScopedCoupon_AppliesOnlyToScopedSubtotal()
    {
        // Scoped to Cat1 only; flat €5 discount on the €40 Cat1 subtotal (not €100 total).
        var coupon = ScopedFlat(5m, Cat1);
        var items = new (Guid? CategoryId, decimal LineTotal)[]
        {
            (Cat1, 40m),
            (Cat2, 60m),
        };
        var scopedSubtotal = coupon.ComputeScopedSubtotal(items);
        var result = coupon.CalculateDiscount(scopedSubtotal, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(5m);
    }

    [Fact]
    public void CalculateDiscount_ScopedTiered_PicksTierFromScopedSubtotal()
    {
        var coupon = new Coupon(
            "ST",
            DiscountType.Tiered,
            0m,
            null, null, null, null,
            tiers: [new CouponTier(30m, 8m), new CouponTier(60m, 15m)],
            scopedCategoryIds: [Cat1]);

        var items = new (Guid? CategoryId, decimal LineTotal)[]
        {
            (Cat1, 70m),   // in scope → scoped subtotal = 70, picks €15 tier
            (Cat2, 200m),  // not in scope
        };

        var scopedSubtotal = coupon.ComputeScopedSubtotal(items);
        var result = coupon.CalculateDiscount(scopedSubtotal, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(15m);
    }
}
