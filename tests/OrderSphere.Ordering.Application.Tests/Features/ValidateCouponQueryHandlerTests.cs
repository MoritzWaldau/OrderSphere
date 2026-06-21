using OrderSphere.Ordering.Application.Features.Coupon;

namespace OrderSphere.Ordering.Application.Tests.Features;

public sealed class ValidateCouponQueryHandlerTests
{
    private static ValidateCouponQueryHandler CreateHandler()
    {
        // Seed the two codes the former hardcoded handler supported.
        var coupons = new List<Coupon>
        {
            new("WELCOME10", DiscountType.Flat, 10m, minSubtotal: null,
                validFrom: null, validUntil: null, maxRedemptions: null, isActive: true),
            new("SUMMER15", DiscountType.Flat, 15m, minSubtotal: 100m,
                validFrom: null, validUntil: null, maxRedemptions: null, isActive: true),
        }.BuildMockDbSet();

        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.Coupons.Returns(coupons);

        return new ValidateCouponQueryHandler(ctx, Substitute.For<ILogger<ValidateCouponQueryHandler>>());
    }


    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_EmptyOrWhitespaceCode_ReturnsCodeRequiredError(string? code)
    {
        var result = await CreateHandler().Handle(new(code!, 50m), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.CodeRequired);
    }


    [Fact]
    public async Task Handle_UnknownCode_ReturnsInvalidCodeError()
    {
        var result = await CreateHandler().Handle(new("UNKNOWN", 50m), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.InvalidCode);
    }


    [Fact]
    public async Task Handle_Summer15_SubtotalBelowMinimum_ReturnsInvalidCouponDto()
    {
        // SUMMER15 requires MinSubtotal = 100m
        var result = await CreateHandler().Handle(new("SUMMER15", 80m), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.DiscountAmount.Should().Be(0m);
    }


    [Fact]
    public async Task Handle_Welcome10_ValidSubtotal_ReturnsDiscount10()
    {
        var result = await CreateHandler().Handle(new("WELCOME10", 50m), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.DiscountAmount.Should().Be(10m);
    }


    [Fact]
    public async Task Handle_Welcome10_SubtotalLessThanDiscount_CappsAtSubtotal()
    {
        // Subtotal = 8, discount = 10 → capped to 8
        var result = await CreateHandler().Handle(new("WELCOME10", 8m), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.DiscountAmount.Should().Be(8m);
    }


    [Fact]
    public async Task Handle_LowercaseCode_ResolvesCaseInsensitively()
    {
        var result = await CreateHandler().Handle(new("welcome10", 50m), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
    }


    [Fact]
    public async Task Handle_Summer15_SubtotalAboveMinimum_ReturnsDiscount15()
    {
        var result = await CreateHandler().Handle(new("SUMMER15", 150m), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.DiscountAmount.Should().Be(15m);
    }
}
