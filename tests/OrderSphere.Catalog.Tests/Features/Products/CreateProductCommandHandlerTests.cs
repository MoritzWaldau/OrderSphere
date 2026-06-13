using MockQueryable.NSubstitute;
using OrderSphere.Catalog.Application.Features.Products.Admin.CreateProduct;

namespace OrderSphere.Catalog.Tests.Features.Products;

public sealed class CreateProductCommandHandlerTests
{
    // ── Shared test data ────────────────────────────────────────────────────────

    private static readonly CategoryId CategoryA = CategoryId.New();

    private static CreateProductCommand ValidCommand(string sku = "SKU-001") =>
        new("Widget", "A widget", 9.99m, 10, CategoryA, sku, null);

    private static CreateProductCommandHandler CreateHandler(ICatalogDbContext ctx) => new(ctx);

    // ── SKU already exists ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_SkuAlreadyExists_ReturnsSkuAlreadyExistsError()
    {
        var existing = new Product("Old", "desc", Money.Of(5m), 5, CategoryA, "SKU-001");
        var products = new List<Product> { existing }.BuildMockDbSet();
        var categories = new List<Category>().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Products.Returns(products);
        ctx.Categories.Returns(categories);

        var result = await CreateHandler(ctx).Handle(ValidCommand("SKU-001"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.SkuAlreadyExists);
    }

    // ── Category not found ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CategoryNotFound_ReturnsCategoryNotFoundError()
    {
        var products = new List<Product>().BuildMockDbSet();
        var categories = new List<Category>().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Products.Returns(products);
        ctx.Categories.Returns(categories);

        var result = await CreateHandler(ctx).Handle(ValidCommand(), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CategoryErrors.NotFound);
    }

    // ── Happy path ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithNonEmptyGuid()
    {
        var cat = MakeCategory(CategoryA);
        var products = new List<Product>().BuildMockDbSet();
        var categories = new List<Category> { cat }.BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Products.Returns(products);
        ctx.Categories.Returns(categories);

        var result = await CreateHandler(ctx).Handle(ValidCommand(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSaveChanges()
    {
        var cat = MakeCategory(CategoryA);
        var products = new List<Product>().BuildMockDbSet();
        var categories = new List<Category> { cat }.BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Products.Returns(products);
        ctx.Categories.Returns(categories);

        await CreateHandler(ctx).Handle(ValidCommand(), default);

        await ctx.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    /// <summary>Creates a Category with a controlled Id for query matching.</summary>
    private static Category MakeCategory(CategoryId id)
    {
        var cat = new Category("Electronics");
        cat.Id = id;
        return cat;
    }
}
