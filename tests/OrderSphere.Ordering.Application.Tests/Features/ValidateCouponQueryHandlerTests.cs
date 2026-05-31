using OrderSphere.Ordering.Application.Features.Coupon;
using Microsoft.Extensions.Logging;

namespace OrderSphere.Ordering.Application.Tests.Features;

public sealed class ValidateCouponQueryHandlerTests
{
    private static ValidateCouponQueryHandler CreateHandler() =>
        new(Substitute.For<ILogger<ValidateCouponQueryHandler>>());

    // ── Code required ────────────────────────────────────────────────────────────

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

    // ── Code not found ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UnknownCode_ReturnsInvalidCodeError()
    {
        var result = await CreateHandler().Handle(new("UNKNOWN", 50m), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.InvalidCode);
    }

    // ── Minimum subtotal not reached ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_Summer15_SubtotalBelowMinimum_ReturnsInvalidCouponDto()
    {
        // SUMMER15 requires MinSubtotal = 100m
        var result = await CreateHandler().Handle(new("SUMMER15", 80m), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.DiscountAmount.Should().Be(0m);
    }

    // ── Happy path — flat discount ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Welcome10_ValidSubtotal_ReturnsDiscount10()
    {
        var result = await CreateHandler().Handle(new("WELCOME10", 50m), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.DiscountAmount.Should().Be(10m);
    }

    // ── Happy path — discount capped at subtotal ──────────────────────────────────

    [Fact]
    public async Task Handle_Welcome10_SubtotalLessThanDiscount_CappsAtSubtotal()
    {
        // Subtotal = 8, discount = 10 → capped to 8
        var result = await CreateHandler().Handle(new("WELCOME10", 8m), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.DiscountAmount.Should().Be(8m);
    }

    // ── Case insensitivity ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_LowercaseCode_ResolvesCaseInsensitively()
    {
        var result = await CreateHandler().Handle(new("welcome10", 50m), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
    }

    // ── SUMMER15 happy path ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Summer15_SubtotalAboveMinimum_ReturnsDiscount15()
    {
        var result = await CreateHandler().Handle(new("SUMMER15", 150m), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.DiscountAmount.Should().Be(15m);
    }
}
