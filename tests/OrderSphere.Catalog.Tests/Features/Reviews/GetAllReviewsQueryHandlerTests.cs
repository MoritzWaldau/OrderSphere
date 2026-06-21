using OrderSphere.Catalog.Application.Features.Reviews.Admin.GetAllReviews;
using OrderSphere.Catalog.Tests.Helpers;

namespace OrderSphere.Catalog.Tests.Features.Reviews;

public sealed class GetAllReviewsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllReviews_AcrossStatuses()
    {
        await using var ctx = CatalogDbContextFactory.Create();
        var category = new Category("Shoes");
        var product = new Product("Air", "d", Money.Of(10m), 5, category.Id, "SKU-1");
        ctx.Categories.Add(category);
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();

        var approved = ProductReview.Create(product.Id, CustomerId.New(), 5, "Great", "Loved it.", true).Value;
        var rejected = ProductReview.Create(product.Id, CustomerId.New(), 1, "Bad", "Spam content.", false).Value;
        rejected.Reject();
        ctx.Reviews.AddRange(approved, rejected);
        await ctx.SaveChangesAsync();

        var result = await new GetAllReviewsQueryHandler(ctx).Handle(new(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(r => r.Status).Should().Contain(new[] { "Approved", "Rejected" });
        result.Value.Should().Contain(r => r.Title == "Great" && r.IsVerifiedPurchase);
    }

    [Fact]
    public async Task Handle_NoReviews_ReturnsEmptySuccess()
    {
        await using var ctx = CatalogDbContextFactory.Create();

        var result = await new GetAllReviewsQueryHandler(ctx).Handle(new(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
