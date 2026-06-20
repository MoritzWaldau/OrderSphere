using MockQueryable.NSubstitute;
using OrderSphere.Catalog.Application.Features.Brands.Admin.CreateBrand;

namespace OrderSphere.Catalog.Tests.Features.Brands;

public sealed class CreateBrandCommandHandlerTests
{
    private static CreateBrandCommandHandler CreateHandler(ICatalogDbContext ctx) => new(ctx);

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithNonEmptyGuid()
    {
        var brands = new List<Brand>().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Brands.Returns(brands);

        var result = await CreateHandler(ctx).Handle(new("Apple", "Maker of iPhones"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsBrandToDbSet()
    {
        var brands = new List<Brand>().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Brands.Returns(brands);

        await CreateHandler(ctx).Handle(new("Samsung", ""), default);

        ctx.Brands.Received(1).Add(Arg.Is<Brand>(b => b.Name == "Samsung"));
        await ctx.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsNameAlreadyExistsError()
    {
        var existing = new Brand("Apple", "desc");
        var brands = new List<Brand> { existing }.BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Brands.Returns(brands);

        var result = await CreateHandler(ctx).Handle(new("Apple", "again"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BrandErrors.NameAlreadyExists);
    }
}
