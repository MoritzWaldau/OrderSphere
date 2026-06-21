using MockQueryable.NSubstitute;
using OrderSphere.Catalog.Application.Features.Categories.Admin.UpdateCategory;
using OrderSphere.Catalog.Tests.Helpers;

namespace OrderSphere.Catalog.Tests.Features.Categories;

public sealed class UpdateCategoryCommandHandlerTests
{
    private static readonly CategoryId CategoryA = CategoryId.New();

    private static UpdateCategoryCommandHandler CreateHandler(ICatalogDbContext ctx) => new(ctx);


    [Fact]
    public async Task Handle_CategoryNotFound_ReturnsNotFoundError()
    {
        var categories = new List<Category>().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Categories.Returns(categories);

        var result = await CreateHandler(ctx).Handle(
            new(CategoryA, "Updated", "desc", true), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CategoryErrors.NotFound);
    }


    [Fact]
    public async Task Handle_CategoryIsDeleted_ReturnsNotFoundError()
    {
        await using var ctx = CatalogDbContextFactory.Create();
        var cat = MakeCategory(CategoryA);
        ctx.Categories.Add(cat);
        await ctx.SaveChangesAsync();
        cat.IsDeleted = true;
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(
            new(CategoryA, "Updated", "desc", true), default);

        result.IsFailure.Should().BeTrue();
    }


    [Fact]
    public async Task Handle_ExistingCategory_IsActive_ActivatesCategory()
    {
        var cat = MakeCategory(CategoryA, active: false);
        var categories = new List<Category> { cat }.BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Categories.Returns(categories);

        var result = await CreateHandler(ctx).Handle(
            new(CategoryA, "Updated", "desc", IsActive: true), default);

        result.IsSuccess.Should().BeTrue();
        cat.IsActive.Should().BeTrue();
    }


    [Fact]
    public async Task Handle_ExistingCategory_IsNotActive_DeactivatesCategory()
    {
        var cat = MakeCategory(CategoryA, active: true);
        var categories = new List<Category> { cat }.BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Categories.Returns(categories);

        var result = await CreateHandler(ctx).Handle(
            new(CategoryA, "Updated", "desc", IsActive: false), default);

        result.IsSuccess.Should().BeTrue();
        cat.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ExistingCategory_UpdatesNameAndDescription()
    {
        var cat = MakeCategory(CategoryA);
        var categories = new List<Category> { cat }.BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Categories.Returns(categories);

        await CreateHandler(ctx).Handle(
            new(CategoryA, "New Name", "New Desc", true), default);

        cat.Name.Should().Be("New Name");
        cat.Description.Should().Be("New Desc");
    }

    [Fact]
    public async Task Handle_ExistingCategory_CallsSaveChanges()
    {
        var cat = MakeCategory(CategoryA);
        var categories = new List<Category> { cat }.BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Categories.Returns(categories);

        await CreateHandler(ctx).Handle(
            new(CategoryA, "Name", "Desc", true), default);

        await ctx.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }


    private static Category MakeCategory(CategoryId id, bool active = true)
    {
        var cat = new Category("Electronics");
        cat.Id = id;
        if (!active) cat.Deactivate();
        return cat;
    }
}
