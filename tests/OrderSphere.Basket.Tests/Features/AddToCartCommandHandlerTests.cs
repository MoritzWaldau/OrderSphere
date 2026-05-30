using OrderSphere.Basket.Tests.Helpers;

namespace OrderSphere.Basket.Tests.Features;

public sealed class AddToCartCommandHandlerTests
{
    // ── Shared test data ────────────────────────────────────────────────────────

    private static readonly CustomerId Customer  = CustomerId.New();
    private static readonly ProductId  ProductA  = ProductId.New();

    private static AddToCartCommand ValidCommand(int qty = 2) =>
        new(Customer, ProductA, qty);

    private static CatalogProductInfo AvailableProduct(int stock = 10) =>
        new(ProductA.Value, "Widget", 9.99m, stock, IsActive: true);

    private static AddToCartCommandHandler CreateHandler(
        BasketDbContext ctx,
        ICatalogClient? catalogClient = null,
        ILogger<AddToCartCommandHandler>? logger = null)
    {
        catalogClient ??= Substitute.For<ICatalogClient>();
        logger        ??= Substitute.For<ILogger<AddToCartCommandHandler>>();
        return new AddToCartCommandHandler(ctx, catalogClient, logger);
    }

    // ── Product not found in Catalog ────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProductNotFoundInCatalog_ReturnsProductNotFoundError()
    {
        using var ctx = BasketDbContextFactory.Create();
        var catalog = Substitute.For<ICatalogClient>();
        catalog.GetProductByIdAsync(ProductA.Value, Arg.Any<CancellationToken>())
               .Returns(Result<CatalogProductInfo>.Failure(ProductErrors.ProductNotFoundError));

        var result = await CreateHandler(ctx, catalog).Handle(ValidCommand(), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.ProductNotFoundError);
    }

    // ── Insufficient stock ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_InsufficientStock_ReturnsInsufficientStockError()
    {
        using var ctx = BasketDbContextFactory.Create();
        var catalog = Substitute.For<ICatalogClient>();
        catalog.GetProductByIdAsync(ProductA.Value, Arg.Any<CancellationToken>())
               .Returns(Result<CatalogProductInfo>.Success(AvailableProduct(stock: 1)));

        var result = await CreateHandler(ctx, catalog).Handle(ValidCommand(qty: 5), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.InsufficientStockError);
    }

    // ── New cart created ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoExistingCart_CreatesNewCartWithItem()
    {
        using var ctx = BasketDbContextFactory.Create();
        var catalog = Substitute.For<ICatalogClient>();
        catalog.GetProductByIdAsync(ProductA.Value, Arg.Any<CancellationToken>())
               .Returns(Result<CatalogProductInfo>.Success(AvailableProduct()));

        var result = await CreateHandler(ctx, catalog).Handle(ValidCommand(qty: 2), default);

        result.IsSuccess.Should().BeTrue();
        var cart = await ctx.Carts.Include(c => c.Items)
                              .FirstOrDefaultAsync(c => c.CustomerId == Customer);
        cart.Should().NotBeNull();
        cart!.Items.Should().ContainSingle(i => i.ProductId == ProductA);
    }

    // ── Existing cart updated ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExistingCart_IncreasesQuantity()
    {
        using var ctx = BasketDbContextFactory.Create();

        // Seed an existing cart with one item
        var cart = new Cart(Customer);
        cart.AddItem(new CartItem(ProductA, Quantity.Of(3)));
        await ctx.Carts.AddAsync(cart);
        await ctx.SaveChangesAsync();

        var catalog = Substitute.For<ICatalogClient>();
        catalog.GetProductByIdAsync(ProductA.Value, Arg.Any<CancellationToken>())
               .Returns(Result<CatalogProductInfo>.Success(AvailableProduct(stock: 20)));

        var result = await CreateHandler(ctx, catalog).Handle(ValidCommand(qty: 2), default);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await ctx.Carts.Include(c => c.Items)
                                      .FirstOrDefaultAsync(c => c.CustomerId == Customer);
        reloaded!.Items.Single(i => i.ProductId == ProductA).Quantity.Value.Should().Be(5);
    }

    // ── Exception path — propagates when catalog client throws ──────────────────

    [Fact]
    public async Task Handle_CatalogClientThrows_PropagatesException()
    {
        using var ctx = BasketDbContextFactory.Create();
        var catalog = Substitute.For<ICatalogClient>();
        catalog.GetProductByIdAsync(ProductA.Value, Arg.Any<CancellationToken>())
               .ThrowsAsync(new HttpRequestException("network error"));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateHandler(ctx, catalog).Handle(ValidCommand(), default));
    }
}
