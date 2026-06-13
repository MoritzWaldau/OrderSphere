using MockQueryable.NSubstitute;
using OrderSphere.Catalog.Application.Features.Products.Admin.DeleteProduct;
using OrderSphere.Catalog.Tests.Helpers;

namespace OrderSphere.Catalog.Tests.Features.Products;

public sealed class DeleteProductCommandHandlerTests
{
    private static readonly CategoryId CategoryA = CategoryId.New();
    private static readonly ProductId ProductA = ProductId.New();

    private static DeleteProductCommand ValidCommand() => new(ProductA);

    private static DeleteProductCommandHandler CreateHandler(ICatalogDbContext ctx) => new(ctx);

    // ── Product not found ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsNotFoundError()
    {
        var products = new List<Product>().AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Products.Returns(products);

        var result = await CreateHandler(ctx).Handle(ValidCommand(), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NotFound);
    }

    // ── Already soft-deleted ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProductAlreadyDeleted_ReturnsNotFoundError()
    {
        await using var ctx = CatalogDbContextFactory.Create();
        var category = new Category("Electronics");
        category.Id = CategoryA;
        ctx.Categories.Add(category);
        var product = MakeProduct(ProductA);
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();
        product.IsDeleted = true;
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(ValidCommand(), default);

        result.IsFailure.Should().BeTrue();
    }

    // ── Happy path ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExistingProduct_SetsIsDeletedTrue()
    {
        var product = MakeProduct(ProductA);
        var products = new List<Product> { product }.AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Products.Returns(products);

        var result = await CreateHandler(ctx).Handle(ValidCommand(), default);

        result.IsSuccess.Should().BeTrue();
        product.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ExistingProduct_CallsSaveChanges()
    {
        var product = MakeProduct(ProductA);
        var products = new List<Product> { product }.AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Products.Returns(products);

        await CreateHandler(ctx).Handle(ValidCommand(), default);

        await ctx.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static Product MakeProduct(ProductId id)
    {
        var p = new Product("Widget", "desc", Money.Of(9.99m), 10, CategoryA, "SKU-001");
        p.Id = id;
        return p;
    }
}
