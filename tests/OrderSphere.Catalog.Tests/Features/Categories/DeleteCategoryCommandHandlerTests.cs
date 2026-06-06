using MockQueryable.NSubstitute;
using OrderSphere.Catalog.Application.Features.Categories.Admin.DeleteCategory;

namespace OrderSphere.Catalog.Tests.Features.Categories;

public sealed class DeleteCategoryCommandHandlerTests
{
    private static readonly CategoryId CategoryA = CategoryId.New();
    private static readonly ProductId ProductA = ProductId.New();

    private static DeleteCategoryCommandHandler CreateHandler(ICatalogDbContext ctx) => new(ctx);

    // ── Category not found ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CategoryNotFound_ReturnsNotFoundError()
    {
        var categories = new List<Category>().AsQueryable().BuildMockDbSet();
        var products = new List<Product>().AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Categories.Returns(categories);
        ctx.Products.Returns(products);

        var result = await CreateHandler(ctx).Handle(new(CategoryA), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CategoryErrors.NotFound);
    }

    // ── Category has active products ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_CategoryHasProducts_ReturnsHasProductsError()
    {
        var cat = MakeCategory(CategoryA);
        var product = new Product("Widget", "desc", Money.Of(9.99m), 5, CategoryA, "SKU-001");
        product.Id = ProductA;

        var categories = new List<Category> { cat }.AsQueryable().BuildMockDbSet();
        var products = new List<Product> { product }.AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Categories.Returns(categories);
        ctx.Products.Returns(products);

        var result = await CreateHandler(ctx).Handle(new(CategoryA), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CategoryErrors.HasProducts);
    }

    // ── Happy path ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CategoryWithNoProducts_SetsIsDeletedTrue()
    {
        var cat = MakeCategory(CategoryA);
        var categories = new List<Category> { cat }.AsQueryable().BuildMockDbSet();
        var products = new List<Product>().AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Categories.Returns(categories);
        ctx.Products.Returns(products);

        var result = await CreateHandler(ctx).Handle(new(CategoryA), default);

        result.IsSuccess.Should().BeTrue();
        cat.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_CategoryWithNoProducts_CallsSaveChanges()
    {
        var cat = MakeCategory(CategoryA);
        var categories = new List<Category> { cat }.AsQueryable().BuildMockDbSet();
        var products = new List<Product>().AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Categories.Returns(categories);
        ctx.Products.Returns(products);

        await CreateHandler(ctx).Handle(new(CategoryA), default);

        await ctx.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static Category MakeCategory(CategoryId id)
    {
        var cat = new Category("Electronics");
        cat.Id = id;
        return cat;
    }
}
