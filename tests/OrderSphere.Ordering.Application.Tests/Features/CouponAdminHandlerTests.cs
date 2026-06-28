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
    public async Task CreateCoupon_WithTiers_PersistsTiers()
    {
        await using var ctx = OrderingDbContextFactory.Create();
        var tiers = new List<CreateCouponTierDto>
        {
            new(50m, 5m),
            new(100m, 15m),
        };

        var result = await new CreateCouponCommandHandler(ctx)
            .Handle(new("TIERED", (int)DiscountType.Tiered, 0m, null, null, null, null, true, tiers), default);

        result.IsSuccess.Should().BeTrue();
        var stored = await ctx.Coupons.SingleAsync(c => c.Code == "TIERED");
        stored.Tiers.Should().HaveCount(2);
        stored.Tiers.Should().ContainSingle(t => t.MinSubtotal == 50m && t.DiscountValue == 5m);
        stored.Tiers.Should().ContainSingle(t => t.MinSubtotal == 100m && t.DiscountValue == 15m);
    }

    [Fact]
    public async Task CreateCoupon_WithScopedCategories_PersistsCategories()
    {
        await using var ctx = OrderingDbContextFactory.Create();
        var catId = new Guid("aaaaaaaa-0000-0000-0000-000000000001");

        var result = await new CreateCouponCommandHandler(ctx)
            .Handle(new("SCOPED", (int)DiscountType.Flat, 10m, null, null, null, null, true,
                ScopedCategoryIds: [catId]), default);

        result.IsSuccess.Should().BeTrue();
        var stored = await ctx.Coupons.SingleAsync(c => c.Code == "SCOPED");
        stored.ScopedCategoryIds.Should().ContainSingle().Which.Should().Be(catId);
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
    public async Task UpdateCoupon_AddsTiers_ReplacesPreviousTiers()
    {
        await using var ctx = OrderingDbContextFactory.Create();
        var coupon = new Coupon("TIER2", DiscountType.Tiered, 0m, null, null, null, null,
            tiers: [new(50m, 5m)]);
        ctx.Coupons.Add(coupon);
        await ctx.SaveChangesAsync();

        var newTiers = new List<CreateCouponTierDto> { new(100m, 20m), new(200m, 40m) };
        var result = await new UpdateCouponCommandHandler(ctx)
            .Handle(new(coupon.Id.Value, (int)DiscountType.Tiered, 0m, null, null, null, null, true, newTiers), default);

        result.IsSuccess.Should().BeTrue();
        var updated = await ctx.Coupons.SingleAsync(c => c.Id == coupon.Id);
        updated.Tiers.Should().HaveCount(2);
        updated.Tiers.Should().NotContain(t => t.MinSubtotal == 50m);
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
