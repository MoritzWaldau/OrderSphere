using FluentAssertions;
using OrderSphere.Ordering.Domain.Entities;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.Errors;
using Xunit;

namespace OrderSphere.Domain.Tests.Aggregates;

public sealed class CouponTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Coupon Flat(decimal value, decimal? min = null, bool active = true) =>
        new("SAVE", DiscountType.Flat, value, min, null, null, null, active);


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
}
