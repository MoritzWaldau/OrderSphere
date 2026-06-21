using Microsoft.EntityFrameworkCore;
using OrderSphere.Ordering.Application.Features.Coupon.Admin.CreateCoupon;
using OrderSphere.Ordering.Application.Features.Coupon.Admin.DeactivateCoupon;
using OrderSphere.Ordering.Application.Features.Coupon.Admin.GetAllCoupons;
using OrderSphere.Ordering.Application.Features.Coupon.Admin.UpdateCoupon;
using OrderSphere.Ordering.Application.Tests.Helpers;

namespace OrderSphere.Ordering.Application.Tests.Features;

public sealed class CouponAdminHandlerTests
{
    private static Coupon NewCoupon(string code = "SAVE10")
        => new(code, DiscountType.Flat, 10m, null, null, null, null, true);


    [Fact]
    public async Task CreateCoupon_NewCode_PersistsNormalizedUppercaseCode()
    {
        await using var ctx = OrderingDbContextFactory.Create();

        var result = await new CreateCouponCommandHandler(ctx)
            .Handle(new("save10", (int)DiscountType.Flat, 10m, null, null, null, null, true), default);

        result.IsSuccess.Should().BeTrue();
        var stored = await ctx.Coupons.SingleAsync(c => c.Code == "SAVE10");
        stored.Code.Should().Be("SAVE10");
    }

    [Fact]
    public async Task CreateCoupon_DuplicateCode_ReturnsCodeExists()
    {
        await using var ctx = OrderingDbContextFactory.Create();
        ctx.Coupons.Add(NewCoupon("SAVE10"));
        await ctx.SaveChangesAsync();

        var result = await new CreateCouponCommandHandler(ctx)
            .Handle(new("save10", (int)DiscountType.Flat, 10m, null, null, null, null, true), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.CodeExists);
    }


    [Fact]
    public async Task UpdateCoupon_Unknown_ReturnsNotFound()
    {
        await using var ctx = OrderingDbContextFactory.Create();

        var result = await new UpdateCouponCommandHandler(ctx)
            .Handle(new(Guid.NewGuid(), (int)DiscountType.Flat, 5m, null, null, null, null, true), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.NotFound);
    }

    [Fact]
    public async Task UpdateCoupon_Existing_AppliesNewValues()
    {
        await using var ctx = OrderingDbContextFactory.Create();
        var coupon = NewCoupon();
        ctx.Coupons.Add(coupon);
        await ctx.SaveChangesAsync();

        var result = await new UpdateCouponCommandHandler(ctx)
            .Handle(new(coupon.Id.Value, (int)DiscountType.Percentage, 25m, 50m, null, null, 100, false), default);

        result.IsSuccess.Should().BeTrue();
        var updated = await ctx.Coupons.SingleAsync(c => c.Id == coupon.Id);
        updated.DiscountType.Should().Be(DiscountType.Percentage);
        updated.Value.Should().Be(25m);
        updated.IsActive.Should().BeFalse();
    }


    [Fact]
    public async Task DeactivateCoupon_Unknown_ReturnsNotFound()
    {
        await using var ctx = OrderingDbContextFactory.Create();

        var result = await new DeactivateCouponCommandHandler(ctx).Handle(new(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.NotFound);
    }

    [Fact]
    public async Task DeactivateCoupon_Existing_SetsInactive()
    {
        await using var ctx = OrderingDbContextFactory.Create();
        var coupon = NewCoupon();
        ctx.Coupons.Add(coupon);
        await ctx.SaveChangesAsync();

        var result = await new DeactivateCouponCommandHandler(ctx).Handle(new(coupon.Id.Value), default);

        result.IsSuccess.Should().BeTrue();
        (await ctx.Coupons.SingleAsync(c => c.Id == coupon.Id)).IsActive.Should().BeFalse();
    }


    [Fact]
    public async Task GetAllCoupons_ReturnsAllOrderedByCode_WithMappedFields()
    {
        await using var ctx = OrderingDbContextFactory.Create();
        ctx.Coupons.Add(NewCoupon("ZETA"));
        ctx.Coupons.Add(NewCoupon("ALPHA"));
        await ctx.SaveChangesAsync();

        var result = await new GetAllCouponsQueryHandler(ctx).Handle(new(), default);

        result.IsSuccess.Should().BeTrue();
        // Ordered ascending by code (seeded coupons WELCOME10/SUMMER15 sort between these two).
        result.Value.Select(c => c.Code).Should().ContainInOrder("ALPHA", "ZETA");
        result.Value.Should().Contain(c => c.Code == "ALPHA" && c.DiscountType == (int)DiscountType.Flat && c.Value == 10m);
    }
}
