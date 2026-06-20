using Microsoft.EntityFrameworkCore;
using OrderSphere.Catalog.Application.Features.Brands.Admin.DeleteBrand;
using OrderSphere.Catalog.Tests.Helpers;

namespace OrderSphere.Catalog.Tests.Features.Brands;

public sealed class DeleteBrandCommandHandlerTests
{
    [Fact]
    public async Task Handle_BrandNotFound_ReturnsNotFound()
    {
        await using var ctx = CatalogDbContextFactory.Create();

        var result = await new DeleteBrandCommandHandler(ctx).Handle(new(BrandId.New()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BrandErrors.NotFound);
    }

    [Fact]
    public async Task Handle_BrandHasProducts_ReturnsHasProducts()
    {
        await using var ctx = CatalogDbContextFactory.Create();
        var category = new Category("Shoes");
        var brand = new Brand("Nike");
        ctx.Categories.Add(category);
        ctx.Brands.Add(brand);
        ctx.Products.Add(new Product("Air", "d", Money.Of(10m), 5, category.Id, "SKU-1", brandId: brand.Id));
        await ctx.SaveChangesAsync();

        var result = await new DeleteBrandCommandHandler(ctx).Handle(new(brand.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BrandErrors.HasProducts);
    }

    [Fact]
    public async Task Handle_ExistingBrandWithoutProducts_SoftDeletes()
    {
        await using var ctx = CatalogDbContextFactory.Create();
        var brand = new Brand("Nike");
        ctx.Brands.Add(brand);
        await ctx.SaveChangesAsync();

        var result = await new DeleteBrandCommandHandler(ctx).Handle(new(brand.Id), default);

        result.IsSuccess.Should().BeTrue();
        var stored = await ctx.Brands.IgnoreQueryFilters().SingleAsync(b => b.Id == brand.Id);
        stored.IsDeleted.Should().BeTrue();
    }
}
