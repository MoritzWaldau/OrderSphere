using FluentAssertions;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.BuildingBlocks.ValueObjects;
using OrderSphere.Catalog.Domain.Entities;
using OrderSphere.Catalog.Domain.Enums;
using OrderSphere.Catalog.Domain.Errors;
using Xunit;

namespace OrderSphere.Domain.Tests.Aggregates;

public sealed class ProductReviewTests
{
    private static readonly ProductId Product = ProductId.New();
    private static readonly CustomerId Customer = CustomerId.New();

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Create_ValidRating_Succeeds(int rating)
    {
        var result = ProductReview.Create(Product, Customer, rating, " Great ", " Body text ", isVerifiedPurchase: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rating.Should().Be(rating);
        result.Value.IsVerifiedPurchase.Should().BeTrue();
        result.Value.Status.Should().Be(ReviewStatus.Approved);
        result.Value.Title.Should().Be("Great");   // trimmed
        result.Value.Body.Should().Be("Body text"); // trimmed
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Create_RatingOutOfRange_ReturnsInvalidRating(int rating)
    {
        var result = ProductReview.Create(Product, Customer, rating, "Title", "Body", isVerifiedPurchase: true);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewErrors.InvalidRating);
    }

    [Fact]
    public void Reject_then_Approve_TogglesStatus()
    {
        var review = ProductReview.Create(Product, Customer, 4, "Title", "Body", true).Value;

        review.Reject();
        review.Status.Should().Be(ReviewStatus.Rejected);

        review.Approve();
        review.Status.Should().Be(ReviewStatus.Approved);
    }

    [Theory]
    [InlineData(new[] { 5, 4 }, 4.5, 2)]
    [InlineData(new[] { 5, 4, 4 }, 4.3, 3)]   // 4.333 → 4.3
    [InlineData(new[] { 3, 4 }, 3.5, 2)]
    public void SetRatingSummary_RoundsToOneDecimal(int[] ratings, double expectedAvg, int expectedCount)
    {
        var product = new Product("Widget", "desc", Money.Of(9.99m), 10, CategoryId.New(), "SKU-RV-1");

        product.SetRatingSummary(ratings.Average(), ratings.Length);

        product.AverageRating.Should().Be(expectedAvg);
        product.ReviewCount.Should().Be(expectedCount);
    }
}
