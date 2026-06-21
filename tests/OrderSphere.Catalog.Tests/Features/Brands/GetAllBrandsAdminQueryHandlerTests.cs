using OrderSphere.Catalog.Application.Features.Brands.Admin.GetAllBrandsAdmin;
using OrderSphere.Catalog.Tests.Helpers;

namespace OrderSphere.Catalog.Tests.Features.Brands;

public sealed class GetAllBrandsAdminQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllBrandsIncludingInactive_OrderedByName()
    {
        await using var ctx = CatalogDbContextFactory.Create();
        var puma = new Brand("Puma");
        puma.Deactivate();
        ctx.Brands.AddRange(new Brand("Nike"), puma);
        await ctx.SaveChangesAsync();

        var result = await new GetAllBrandsAdminQueryHandler(ctx).Handle(new(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(b => b.Name).Should().ContainInOrder("Nike", "Puma");
        result.Value.Should().Contain(b => b.Name == "Puma" && !b.IsActive);
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
        await ctx.SaveChangesAsync();

        var result = await new GetAllBrandsAdminQueryHandler(ctx).Handle(new(), default);

        result.Value.Single().ProductCount.Should().Be(1);
    }
}
