using OrderSphere.Catalog.Application.Features.Brands.Public.GetBrands;
using OrderSphere.Catalog.Tests.Helpers;

namespace OrderSphere.Catalog.Tests.Features.Brands;

public sealed class GetBrandsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyActiveBrands_OrderedByName()
    {
        await using var ctx = CatalogDbContextFactory.Create();
        var puma = new Brand("Puma");
        puma.Deactivate();
        ctx.Brands.AddRange(new Brand("Nike"), new Brand("Adidas"), puma);
        await ctx.SaveChangesAsync();

        var result = await new GetBrandsQueryHandler(ctx).Handle(new(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(b => b.Name).Should().ContainInOrder("Adidas", "Nike");
        result.Value.Items.Should().NotContain(b => b.Name == "Puma");
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ProjectsProductCountPerBrand()
    {
        await using var ctx = CatalogDbContextFactory.Create();
        var category = new Category("Shoes");
        var nike = new Brand("Nike");
        ctx.Categories.Add(category);
        ctx.Brands.Add(nike);
        ctx.Products.Add(new Product("Air", "d", Money.Of(10m), 5, category.Id, "SKU-1", brandId: nike.Id));
        ctx.Products.Add(new Product("Max", "d", Money.Of(10m), 5, category.Id, "SKU-2", brandId: nike.Id));
        await ctx.SaveChangesAsync();

        var result = await new GetBrandsQueryHandler(ctx).Handle(new(), default);

        result.Value.Items.Single().ProductCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_Pagination_RespectsPageAndPageSize()
    {
        await using var ctx = CatalogDbContextFactory.Create();
        foreach (var name in new[] { "B", "A", "D", "C" })
            ctx.Brands.Add(new Brand(name));
        await ctx.SaveChangesAsync();

        var result = await new GetBrandsQueryHandler(ctx).Handle(new(Page: 2, PageSize: 2), default);

        result.Value.Items.Select(b => b.Name).Should().ContainInOrder("C", "D");
        result.Value.TotalCount.Should().Be(4);
    }
}
