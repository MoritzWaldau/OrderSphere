using MockQueryable.NSubstitute;
using OrderSphere.Catalog.Application.Features.Products.Admin.UpdateProduct;

namespace OrderSphere.Catalog.Tests.Features.Products;

public sealed class UpdateProductCommandHandlerTests
{
    private static readonly CategoryId CategoryA = CategoryId.New();
    private static readonly ProductId ProductA = ProductId.New();

    private static UpdateProductCommand ValidCommand(bool isActive = true) =>
        new(ProductA, "Updated Widget", "desc", 12.99m, 20, CategoryA, "SKU-002", isActive, null);

    private static UpdateProductCommandHandler CreateHandler(ICatalogDbContext ctx)
        => new(ctx, DisabledProductSearchIndex.Instance);


    [Fact]
    public async Task Handle_ProductNotFound_ReturnsNotFoundError()
    {
        var products = new List<Product>().BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Products.Returns(products);

        var result = await CreateHandler(ctx).Handle(ValidCommand(), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NotFound);
    }


    [Fact]
    public async Task Handle_ExistingProduct_IsActive_ActivatesProduct()
    {
        var product = MakeProduct(ProductA, active: false);
        var products = new List<Product> { product }.BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Products.Returns(products);

        var result = await CreateHandler(ctx).Handle(ValidCommand(isActive: true), default);

        result.IsSuccess.Should().BeTrue();
        product.IsActive.Should().BeTrue();
    }


    [Fact]
    public async Task Handle_ExistingProduct_IsNotActive_DeactivatesProduct()
    {
        var product = MakeProduct(ProductA, active: true);
        var products = new List<Product> { product }.BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Products.Returns(products);

        var result = await CreateHandler(ctx).Handle(ValidCommand(isActive: false), default);

        result.IsSuccess.Should().BeTrue();
        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ExistingProduct_UpdatesDetails()
    {
        var product = MakeProduct(ProductA);
        var products = new List<Product> { product }.BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Products.Returns(products);

        await CreateHandler(ctx).Handle(ValidCommand(), default);

        product.Name.Should().Be("Updated Widget");
        product.SKU.Should().Be("SKU-002");
    }

    [Fact]
    public async Task Handle_ExistingProduct_CallsSaveChanges()
    {
        var product = MakeProduct(ProductA);
        var products = new List<Product> { product }.BuildMockDbSet();
        var ctx = Substitute.For<ICatalogDbContext>();
        ctx.Products.Returns(products);

        await CreateHandler(ctx).Handle(ValidCommand(), default);

        await ctx.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }


    private static Product MakeProduct(ProductId id, bool active = true)
    {
        var p = new Product("Widget", "desc", Money.Of(9.99m), 10, CategoryA, "SKU-001");
        p.Id = id;
        if (!active) p.Deactivate();
        p.PopDomainEvents(); // clear construction events
        return p;
    }
}
