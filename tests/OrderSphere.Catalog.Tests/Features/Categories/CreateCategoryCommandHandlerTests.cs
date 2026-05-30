using MockQueryable.NSubstitute;
using OrderSphere.Catalog.Application.Features.Categories.Admin.CreateCategory;

namespace OrderSphere.Catalog.Tests.Features.Categories;

public sealed class CreateCategoryCommandHandlerTests
{
    private static CreateCategoryCommandHandler CreateHandler(ICatalogDbContext ctx) => new(ctx);

    // ── Happy path ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithNonEmptyGuid()
    {
        var categories = new List<Category>().AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Categories.Returns(categories);

        var result = await CreateHandler(ctx).Handle(new("Electronics", "Electronics category"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSaveChanges()
    {
        var categories = new List<Category>().AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Categories.Returns(categories);

        await CreateHandler(ctx).Handle(new("Books", ""), default);

        await ctx.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsCategoryToDbSet()
    {
        var categories = new List<Category>().AsQueryable().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Categories.Returns(categories);

        await CreateHandler(ctx).Handle(new("Books", ""), default);

        ctx.Categories.Received(1).Add(Arg.Is<Category>(c => c.Name == "Books"));
    }
}
