using OrderSphere.Basket.Tests.Helpers;

namespace OrderSphere.Basket.Tests.Features;

public sealed class GetCartQueryHandlerTests
{
    private static readonly CustomerId Customer = CustomerId.New();
    private static readonly ProductId ProductA  = ProductId.New();

    private static GetCartQueryHandler CreateHandler(
        BasketDbContext ctx,
        ICatalogClient? catalog = null,
        ILogger<GetCartQueryHandler>? logger = null)
    {
        catalog ??= Substitute.For<ICatalogClient>();
        logger  ??= Substitute.For<ILogger<GetCartQueryHandler>>();
        return new GetCartQueryHandler(ctx, catalog, logger);
    }

    // ── Cart not found ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CartNotFound_ReturnsCartNotFoundError()
    {
        using var ctx = BasketDbContextFactory.Create();
        var catalog = Substitute.For<ICatalogClient>();

        var result = await CreateHandler(ctx, catalog)
            .Handle(new GetCartQuery(Customer), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CartErrors.CartNotFoundError);
    }

    // ── Catalog names call fails — still returns CartDto with fallback names ───

    [Fact]
    public async Task Handle_CatalogNamesFails_ReturnsCartDtoWithUnknownProductName()
    {
        using var ctx = BasketDbContextFactory.Create();
        var cart = new Cart(Customer);
        cart.AddItem(new CartItem(ProductA, Quantity.Of(2)));
        await ctx.Carts.AddAsync(cart);
        await ctx.SaveChangesAsync();

        var catalog = Substitute.For<ICatalogClient>();
        catalog.GetProductNamesByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
               .Returns(Result<IReadOnlyDictionary<Guid, string>>.Failure(
                   new Error("catalog.unavailable", "unavailable")));

        var result = await CreateHandler(ctx, catalog)
            .Handle(new GetCartQuery(Customer), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(i => i.ProductName == "Unknown Product");
    }

    // ── Happy path — catalog names resolved ─────────────────────────────────────

    [Fact]
    public async Task Handle_CartExists_ReturnsCartDtoWithProductNames()
    {
        using var ctx = BasketDbContextFactory.Create();
        var cart = new Cart(Customer);
        cart.AddItem(new CartItem(ProductA, Quantity.Of(3)));
        await ctx.Carts.AddAsync(cart);
        await ctx.SaveChangesAsync();

        var names = new Dictionary<Guid, string> { [ProductA.Value] = "Super Widget" };
        var catalog = Substitute.For<ICatalogClient>();
        catalog.GetProductNamesByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
               .Returns(Result<IReadOnlyDictionary<Guid, string>>.Success(names));

        var result = await CreateHandler(ctx, catalog)
            .Handle(new GetCartQuery(Customer), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerId.Should().Be(Customer.Value);
        result.Value.Items.Should().ContainSingle(i => i.ProductName == "Super Widget" && i.Quantity == 3);
    }

    // ── Exception path — propagates when catalog client throws ──────────────────

    [Fact]
    public async Task Handle_CatalogClientThrows_PropagatesException()
    {
        using var ctx = BasketDbContextFactory.Create();
        var cart = new Cart(Customer);
        cart.AddItem(new CartItem(ProductA, Quantity.Of(1)));
        await ctx.Carts.AddAsync(cart);
        await ctx.SaveChangesAsync();

        var catalog = Substitute.For<ICatalogClient>();
        catalog.GetProductNamesByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
               .ThrowsAsync(new HttpRequestException("network error"));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateHandler(ctx, catalog).Handle(new GetCartQuery(Customer), default));
    }
}
