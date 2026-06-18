using MockQueryable.NSubstitute;
using OrderSphere.Catalog.Application.Features.Reviews.Admin.ModerateReview;
using OrderSphere.Catalog.Domain.Enums;

namespace OrderSphere.Catalog.Tests.Features.Reviews;

public sealed class ModerateReviewCommandHandlerTests
{
    private static readonly CategoryId CategoryId = CategoryId.New();

    private static Product MakeProduct()
        => new("Widget", "desc", Money.Of(9.99m), 10, CategoryId, "SKU-MOD");

    private static ProductReview MakeApprovedReview(ProductId productId, CustomerId? customerId = null)
        => ProductReview.Create(productId, customerId ?? CustomerId.New(), 4, "Good", "Works well.", true).Value;

    private static (ModerateReviewCommandHandler Handler, ICatalogDbContext Ctx) Build(
        List<ProductReview> reviews, List<Product> products)
    {
        var reviewsSet = reviews.BuildMockDbSet();
        var productsSet = products.BuildMockDbSet();

        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Reviews.Returns(reviewsSet);
        ctx.Products.Returns(productsSet);
        ctx.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        return (new ModerateReviewCommandHandler(ctx), ctx);
    }

    // ── Not found ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ReviewNotFound_ReturnsFailure()
    {
        var (handler, _) = Build([], []);

        var result = await handler.Handle(new ModerateReviewCommand(Guid.NewGuid(), Approve: true), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewErrors.NotFound);
    }

    // ── Approve ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Approve_SetsStatusApproved_AndSaves()
    {
        var product = MakeProduct();
        var review = MakeApprovedReview(product.Id);
        review.Reject(); // start as Rejected so Approve is meaningful

        var (handler, ctx) = Build([review], [product]);

        var result = await handler.Handle(new ModerateReviewCommand(review.Id.Value, Approve: true), default);

        result.IsSuccess.Should().BeTrue();
        review.Status.Should().Be(ReviewStatus.Approved);
        await ctx.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Reject ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Reject_SetsStatusRejected_AndSaves()
    {
        var product = MakeProduct();
        var review = MakeApprovedReview(product.Id);

        var (handler, ctx) = Build([review], [product]);

        var result = await handler.Handle(new ModerateReviewCommand(review.Id.Value, Approve: false), default);

        result.IsSuccess.Should().BeTrue();
        review.Status.Should().Be(ReviewStatus.Rejected);
        await ctx.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
