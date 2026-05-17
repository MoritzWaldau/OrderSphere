using Microsoft.Extensions.Logging.Abstractions;
using OrderSphere.Application.Features.Coupon.ValidateCoupon;
using OrderSphere.Domain.Errors;

namespace OrderSphere.Application.Tests.Features.Coupon;

public class ValidateCouponQueryHandlerTests
{
    private readonly ValidateCouponQueryHandler _handler =
        new(NullLogger<ValidateCouponQueryHandler>.Instance);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WithEmptyCode_ReturnsCodeRequired(string? code)
    {
        var result = await _handler.Handle(new ValidateCouponQuery(code!, Subtotal: 50m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.CodeRequired);
    }

    [Fact]
    public async Task Handle_WithUnknownCode_ReturnsInvalidCode()
    {
        var result = await _handler.Handle(new ValidateCouponQuery("NOT_A_COUPON", Subtotal: 50m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CouponErrors.InvalidCode);
    }

    [Fact]
    public async Task Handle_WithValidCodeAboveMinSubtotal_ReturnsDiscount()
    {
        var result = await _handler.Handle(new ValidateCouponQuery("SUMMER15", Subtotal: 200m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.DiscountAmount.Should().Be(15m);
    }

    [Fact]
    public async Task Handle_WithValidCodeBelowMinSubtotal_ReturnsInvalidWithThresholdMessage()
    {
        var result = await _handler.Handle(new ValidateCouponQuery("SUMMER15", Subtotal: 50m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.DiscountAmount.Should().Be(0m);
        result.Value.Message.Should().Contain("100");
    }

    [Fact]
    public async Task Handle_WithDiscountExceedingSubtotal_CapsDiscountAtSubtotal()
    {
        var result = await _handler.Handle(new ValidateCouponQuery("WELCOME10", Subtotal: 4m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.DiscountAmount.Should().Be(4m);
    }
}
