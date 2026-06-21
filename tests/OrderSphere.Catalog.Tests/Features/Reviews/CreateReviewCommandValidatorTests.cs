using OrderSphere.Catalog.Application.Features.Reviews.CreateReview;

namespace OrderSphere.Catalog.Tests.Features.Reviews;

public sealed class CreateReviewCommandValidatorTests
{
    private readonly CreateReviewCommandValidator _validator = new();

    private static CreateReviewCommand Valid()
        => new(Guid.NewGuid(), Guid.NewGuid(), 5, "Great shoe", "Comfortable and durable.");

    [Fact]
    public void Validate_ValidCommand_Passes() => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyProductId_Fails()
        => _validator.Validate(Valid() with { ProductId = Guid.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyCustomerId_Fails()
        => _validator.Validate(Valid() with { CustomerId = Guid.Empty }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Validate_RatingOutOfRange_Fails(int rating)
        => _validator.Validate(Valid() with { Rating = rating }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyTitle_Fails()
        => _validator.Validate(Valid() with { Title = "" }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyBody_Fails()
        => _validator.Validate(Valid() with { Body = "" }).IsValid.Should().BeFalse();
}
