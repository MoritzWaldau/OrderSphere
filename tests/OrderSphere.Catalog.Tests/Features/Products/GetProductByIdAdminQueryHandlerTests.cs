using MockQueryable.NSubstitute;
using OrderSphere.Catalog.Application.Features.Products.Admin.GetProductByIdAdmin;

namespace OrderSphere.Catalog.Tests.Features.Products;

public sealed class GetProductByIdAdminQueryHandlerTests
{
    private static readonly CategoryId CategoryA = CategoryId.New();
    private static readonly ProductId ProductA = ProductId.New();

    private static GetProductByIdAdminQueryHandler CreateHandler(ICatalogDbContext ctx) => new(ctx);

    // ── Product not found ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsNotFoundError()
    {
        var products = new List<Product>().AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Products.Returns(products);

        var result = await CreateHandler(ctx).Handle(new(ProductA), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NotFound);
    }

    // ── Deleted product not returned ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProductIsDeleted_ReturnsNotFoundError()
    {
        var product = MakeProduct(ProductA);
        product.IsDeleted = true;
        var products = new List<Product> { product }.AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Products.Returns(products);

        var result = await CreateHandler(ctx).Handle(new(ProductA), default);

        result.IsFailure.Should().BeTrue();
    }

    // ── Happy path ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExistingProduct_ReturnsAdminProductDto()
    {
        var product = MakeProduct(ProductA);
        var products = new List<Product> { product }.AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Products.Returns(products);

        var result = await CreateHandler(ctx).Handle(new(ProductA), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(ProductA.Value);
        result.Value.Name.Should().Be("Widget");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static Product MakeProduct(ProductId id)
    {
        var cat = new Category("Electronics");
        cat.Id = CategoryA;

        var p = new Product("Widget", "desc", Money.Of(9.99m), 10, CategoryA, "SKU-001");
        p.Id = id;
        p.Category = cat;
        return p;
    }
}
