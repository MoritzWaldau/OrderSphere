using MockQueryable.NSubstitute;
using OrderSphere.Catalog.Application.Features.Reviews.Public.GetReviewsForProduct;
using OrderSphere.Catalog.Domain.Enums;

namespace OrderSphere.Catalog.Tests.Features.Reviews;

public sealed class GetReviewsForProductQueryHandlerTests
{
    private static GetReviewsForProductQueryHandler Build(List<ProductReview> reviews)
    {
        var reviewsSet = reviews.BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Reviews.Returns(reviewsSet);
        return new GetReviewsForProductQueryHandler(ctx);
    }

    private static ProductReview MakeReview(ProductId productId, ReviewStatus status)
    {
        var review = ProductReview.Create(productId, CustomerId.New(), 4, "Title", "Body", true).Value;
        if (status == ReviewStatus.Rejected)
            review.Reject();
        return review;
    }

    // ── Happy path ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ReturnsApprovedReviewsForProduct()
    {
        var productId = ProductId.New();
        var approved = MakeReview(productId, ReviewStatus.Approved);

        var handler = Build([approved]);
        var result = await handler.Handle(new GetReviewsForProductQuery(productId.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(r => r.Id == approved.Id.Value);
    }

    // ── Filtering ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExcludesRejectedReviews()
    {
        var productId = ProductId.New();
        var approved = MakeReview(productId, ReviewStatus.Approved);
        var rejected = MakeReview(productId, ReviewStatus.Rejected);

        var handler = Build([approved, rejected]);
        var result = await handler.Handle(new GetReviewsForProductQuery(productId.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Id.Should().Be(approved.Id.Value);
    }

    [Fact]
    public async Task Handle_ExcludesReviewsForOtherProducts()
    {
        var productId = ProductId.New();
        var otherProductId = ProductId.New();
        var forProduct = MakeReview(productId, ReviewStatus.Approved);
        var forOther = MakeReview(otherProductId, ReviewStatus.Approved);

        var handler = Build([forProduct, forOther]);
        var result = await handler.Handle(new GetReviewsForProductQuery(productId.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(r => r.Id == forProduct.Id.Value);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoApprovedReviews()
    {
        var productId = ProductId.New();

        var handler = Build([]);
        var result = await handler.Handle(new GetReviewsForProductQuery(productId.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
