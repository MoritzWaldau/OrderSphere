using Microsoft.EntityFrameworkCore;
using OrderSphere.Catalog.Application.Features.Brands.Admin.UpdateBrand;
using OrderSphere.Catalog.Tests.Helpers;

namespace OrderSphere.Catalog.Tests.Features.Brands;

public sealed class UpdateBrandCommandHandlerTests
{
    [Fact]
    public async Task Handle_BrandNotFound_ReturnsNotFound()
    {
        await using var ctx = CatalogDbContextFactory.Create();

        var result = await new UpdateBrandCommandHandler(ctx)
            .Handle(new(BrandId.New(), "Name", "desc", null, IsActive: true), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BrandErrors.NotFound);
    }

    [Fact]
    public async Task Handle_ExistingBrand_UpdatesDetailsAndActivates()
    {
        await using var ctx = CatalogDbContextFactory.Create();
        var brand = new Brand("Old", "old desc");
        brand.Deactivate();
        ctx.Brands.Add(brand);
        await ctx.SaveChangesAsync();

        var result = await new UpdateBrandCommandHandler(ctx)
            .Handle(new(brand.Id, "New", "new desc", "https://logo", IsActive: true), default);

        result.IsSuccess.Should().BeTrue();
        var updated = await ctx.Brands.SingleAsync(b => b.Id == brand.Id);
        updated.Name.Should().Be("New");
        updated.Description.Should().Be("new desc");
        updated.LogoUrl.Should().Be("https://logo");
        updated.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_IsActiveFalse_DeactivatesBrand()
    {
        await using var ctx = CatalogDbContextFactory.Create();
        var brand = new Brand("Nike");
        ctx.Brands.Add(brand);
        await ctx.SaveChangesAsync();

        await new UpdateBrandCommandHandler(ctx)
            .Handle(new(brand.Id, "Nike", "d", null, IsActive: false), default);

        var updated = await ctx.Brands.SingleAsync(b => b.Id == brand.Id);
        updated.IsActive.Should().BeFalse();
    }
}
