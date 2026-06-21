using OrderSphere.Catalog.Application.Features.Products.Admin.GetAllProductsAdmin;
using OrderSphere.Catalog.Tests.Helpers;

namespace OrderSphere.Catalog.Tests.Features.Products;

public sealed class GetAllProductsAdminQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllProducts_OrderedByName_WithCategoryAndBrand()
    {
        await using var ctx = CatalogDbContextFactory.Create();
        var category = new Category("Shoes");
        var brand = new Brand("Nike");
        ctx.Categories.Add(category);
        ctx.Brands.Add(brand);
        ctx.Products.Add(new Product("Zoom", "d", Money.Of(120m), 3, category.Id, "SKU-Z", brandId: brand.Id));
        ctx.Products.Add(new Product("Air", "d", Money.Of(100m), 5, category.Id, "SKU-A"));
        await ctx.SaveChangesAsync();

        var result = await new GetAllProductsAdminQueryHandler(ctx).Handle(new(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(p => p.Name).Should().ContainInOrder("Air", "Zoom");
        var zoom = result.Value.Single(p => p.Name == "Zoom");
        zoom.CategoryName.Should().Be("Shoes");
        zoom.BrandName.Should().Be("Nike");
    }

    [Fact]
    public async Task Handle_NoProducts_ReturnsEmptySuccess()
    {
        await using var ctx = CatalogDbContextFactory.Create();

        var result = await new GetAllProductsAdminQueryHandler(ctx).Handle(new(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
