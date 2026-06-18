using MockQueryable.NSubstitute;
using OrderSphere.Catalog.Application.Features.Reviews.CreateReview;

namespace OrderSphere.Catalog.Tests.Features.Reviews;

public sealed class CreateReviewCommandHandlerTests
{
    private static readonly CategoryId CategoryA = CategoryId.New();

    private static Product MakeProduct()
    {
        var product = new Product("Widget", "desc", Money.Of(9.99m), 10, CategoryA, "SKU-RV");
        return product;
    }

    private static CreateReviewCommand Command(Guid productId, Guid customerId) =>
        new(productId, customerId, 5, "Excellent", "Works great.");

    private static (CreateReviewCommandHandler Handler, IOrderingClient Ordering, ICatalogDbContext Ctx) Build(
        List<Product> products, List<ProductReview> reviews, bool purchased)
    {
        var productsSet = products.BuildMockDbSet();
        var reviewsSet = reviews.BuildMockDbSet();

        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Products.Returns(productsSet);
        ctx.Reviews.Returns(reviewsSet);

        var ordering = Substitute.For<IOrderingClient>();
        ordering.HasPurchasedAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(purchased));

        return (new CreateReviewCommandHandler(ctx, ordering), ordering, ctx);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsNotFound()
    {
        var (handler, _, _) = Build([], [], purchased: true);

        var result = await handler.Handle(Command(Guid.NewGuid(), Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NotFound);
    }

    [Fact]
    public async Task Handle_AlreadyReviewed_ReturnsAlreadyReviewed()
    {
        var product = MakeProduct();
        var customerId = CustomerId.New();
        var existing = ProductReview.Create(product.Id, customerId, 4, "Old", "Old body", true).Value;

        var (handler, _, _) = Build([product], [existing], purchased: true);

        var result = await handler.Handle(Command(product.Id.Value, customerId.Value), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewErrors.AlreadyReviewed);
    }

    [Fact]
    public async Task Handle_NotPurchased_ReturnsNotPurchased()
    {
        var product = MakeProduct();

        var (handler, _, _) = Build([product], [], purchased: false);

        var result = await handler.Handle(Command(product.Id.Value, Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReviewErrors.NotPurchased);
    }

    [Fact]
    public async Task Handle_ValidVerifiedPurchase_SucceedsAndSaves()
    {
        var product = MakeProduct();

        var (handler, ordering, ctx) = Build([product], [], purchased: true);

        var result = await handler.Handle(Command(product.Id.Value, Guid.NewGuid()), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await ordering.Received(1).HasPurchasedAsync(Arg.Any<Guid>(), product.Id.Value, Arg.Any<CancellationToken>());
        await ctx.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
