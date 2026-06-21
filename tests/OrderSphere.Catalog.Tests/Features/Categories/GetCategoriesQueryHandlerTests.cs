using OrderSphere.Catalog.Application.Features.Categories.Public.GetCategories;
using OrderSphere.Catalog.Tests.Helpers;

namespace OrderSphere.Catalog.Tests.Features.Categories;

public sealed class GetCategoriesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyActiveCategories_OrderedByName()
    {
        await using var ctx = CatalogDbContextFactory.Create();
        var archived = new Category("Archived");
        archived.Deactivate();
        ctx.Categories.AddRange(new Category("Shoes"), new Category("Jackets"), archived);
        await ctx.SaveChangesAsync();

        var result = await new GetCategoriesQueryHandler(ctx).Handle(new(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(c => c.Name).Should().ContainInOrder("Jackets", "Shoes");
        result.Value.Items.Should().NotContain(c => c.Name == "Archived");
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ProjectsProductCountPerCategory()
    {
        await using var ctx = CatalogDbContextFactory.Create();
        var shoes = new Category("Shoes");
        ctx.Categories.Add(shoes);
        ctx.Products.Add(new Product("Air", "d", Money.Of(10m), 5, shoes.Id, "SKU-1"));
        ctx.Products.Add(new Product("Max", "d", Money.Of(10m), 5, shoes.Id, "SKU-2"));
        await ctx.SaveChangesAsync();

        var result = await new GetCategoriesQueryHandler(ctx).Handle(new(), default);

        result.Value.Items.Single().ProductCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_Pagination_RespectsPageAndPageSize()
    {
        await using var ctx = CatalogDbContextFactory.Create();
        foreach (var name in new[] { "B", "A", "D", "C" })
            ctx.Categories.Add(new Category(name));
        await ctx.SaveChangesAsync();

        var result = await new GetCategoriesQueryHandler(ctx).Handle(new(Page: 2, PageSize: 2), default);

        result.Value.Items.Select(c => c.Name).Should().ContainInOrder("C", "D");
        result.Value.TotalCount.Should().Be(4);
    }
}
