using OrderSphere.Catalog.Application.Features.Categories.Admin.GetAllCategoriesAdmin;
using OrderSphere.Catalog.Tests.Helpers;

namespace OrderSphere.Catalog.Tests.Features.Categories;

public sealed class GetAllCategoriesAdminQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllCategoriesIncludingInactive_OrderedByName()
    {
        await using var ctx = CatalogDbContextFactory.Create();
        var archived = new Category("Archived");
        archived.Deactivate();
        ctx.Categories.AddRange(new Category("Shoes"), archived);
        await ctx.SaveChangesAsync();

        var result = await new GetAllCategoriesAdminQueryHandler(ctx).Handle(new(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(c => c.Name).Should().ContainInOrder("Archived", "Shoes");
        result.Value.Should().Contain(c => c.Name == "Archived" && !c.IsActive);
    }

    [Fact]
    public async Task Handle_ProjectsProductCountPerCategory()
    {
        await using var ctx = CatalogDbContextFactory.Create();
        var shoes = new Category("Shoes");
        ctx.Categories.Add(shoes);
        ctx.Products.Add(new Product("Air", "d", Money.Of(10m), 5, shoes.Id, "SKU-1"));
        await ctx.SaveChangesAsync();

        var result = await new GetAllCategoriesAdminQueryHandler(ctx).Handle(new(), default);

        result.Value.Single().ProductCount.Should().Be(1);
    }
}
