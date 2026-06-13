using MockQueryable.NSubstitute;
using OrderSphere.Catalog.Application.Features.Products.Public.GetProducts;

namespace OrderSphere.Catalog.Tests.Features.Products;

public sealed class GetProductsQueryHandlerTests
{
    private static readonly CategoryId ShoesId = CategoryId.New();
    private static readonly CategoryId JacketsId = CategoryId.New();

    private static GetProductsQueryHandler CreateHandler(ICatalogDbContext ctx) => new(ctx);

    private static ICatalogDbContext Context(params Product[] products)
    {
        // Build the mock DbSet before Returns() — NSubstitute cannot configure a
        // substitute that is itself created inside a Returns() call.
        var dbSet = products.AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Products.Returns(dbSet);
        return ctx;
    }

    private static Product MakeProduct(
        string name, decimal price, string categoryName = "Shoes", string? description = null)
    {
        var categoryId = categoryName == "Shoes" ? ShoesId : JacketsId;
        var cat = new Category(categoryName);
        cat.Id = categoryId;

        var p = new Product(name, description ?? $"{name} description",
            Money.Of(price), 10, categoryId, $"SKU-{name}");
        p.Id = ProductId.New();
        p.Category = cat;
        return p;
    }

    // ── Search term ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_SearchTerm_MatchesNameCaseInsensitive()
    {
        var ctx = Context(
            MakeProduct("Trail Runner X1", 99m),
            MakeProduct("Winter Jacket", 149m, "Jackets"));

        var result = await CreateHandler(ctx).Handle(new(SearchTerm: "TRAIL"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(p => p.Name == "Trail Runner X1");
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_SearchTerm_MatchesDescription()
    {
        var ctx = Context(
            MakeProduct("X1", 99m, description: "Lightweight trail shoe"),
            MakeProduct("Winter Jacket", 149m, "Jackets"));

        var result = await CreateHandler(ctx).Handle(new(SearchTerm: "lightweight"), default);

        result.Value.Items.Should().ContainSingle(p => p.Name == "X1");
    }

    // ── Category ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CategoryName_FiltersExactCaseInsensitive()
    {
        var ctx = Context(
            MakeProduct("Trail Runner X1", 99m),
            MakeProduct("Winter Jacket", 149m, "Jackets"));

        var result = await CreateHandler(ctx).Handle(new(CategoryName: "jackets"), default);

        result.Value.Items.Should().ContainSingle(p => p.Name == "Winter Jacket");
    }

    // ── Price range ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_PriceRange_FiltersInclusive()
    {
        var ctx = Context(
            MakeProduct("Budget", 49m),
            MakeProduct("Mid", 100m),
            MakeProduct("Premium", 199m));

        var result = await CreateHandler(ctx).Handle(new(MinPrice: 50m, MaxPrice: 100m), default);

        result.Value.Items.Should().ContainSingle(p => p.Name == "Mid");
    }

    // ── No filters: unchanged default behavior ──────────────────────────────────

    [Fact]
    public async Task Handle_NoFilters_ReturnsAllActiveProducts()
    {
        var ctx = Context(
            MakeProduct("Trail Runner X1", 99m),
            MakeProduct("Winter Jacket", 149m, "Jackets"));

        var result = await CreateHandler(ctx).Handle(new(), default);

        result.Value.TotalCount.Should().Be(2);
    }
}
