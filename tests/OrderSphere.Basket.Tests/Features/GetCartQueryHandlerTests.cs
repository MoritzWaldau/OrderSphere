using OrderSphere.Basket.Tests.Helpers;

namespace OrderSphere.Basket.Tests.Features;

public sealed class GetCartQueryHandlerTests
{
    private static readonly CustomerId Customer = CustomerId.New();
    private static readonly ProductId ProductA  = ProductId.New();

    private static GetCartQueryHandler CreateHandler(
        BasketDbContext ctx,
        ICatalogClient? catalog = null)
    {
        catalog ??= Substitute.For<ICatalogClient>();
        return new GetCartQueryHandler(ctx, catalog);
    }

    private static CatalogProductInfo ProductInfo(string name = "Widget", decimal price = 9.99m) =>
        new(ProductA.Value, name, price, Stock: 10, IsActive: true);

    // ── No cart → 200 with empty items ─────────────────────────────────────────

    [Fact]
    public async Task Handle_CartNotFound_ReturnsEmptyCartDto()
    {
        using var ctx = BasketDbContextFactory.Create();

        var result = await CreateHandler(ctx).Handle(new GetCartQuery(Customer), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.CustomerId.Should().Be(Customer.Value);
    }

    // ── Catalog infos call fails — returns fallback name and zero price ─────────

    [Fact]
    public async Task Handle_CatalogInfosFails_ReturnsCartDtoWithFallbackValues()
    {
        using var ctx = BasketDbContextFactory.Create();
        var cart = new Cart(Customer);
        cart.AddItem(new CartItem(ProductA, Quantity.Of(2)));
        await ctx.Carts.AddAsync(cart);
        await ctx.SaveChangesAsync();

        var catalog = Substitute.For<ICatalogClient>();
        catalog.GetProductInfosByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
               .Returns(Result<IReadOnlyDictionary<Guid, CatalogProductInfo>>.Failure(
                   new Error("catalog.unavailable", "unavailable")));

        var result = await CreateHandler(ctx, catalog).Handle(new GetCartQuery(Customer), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(i =>
            i.ProductName == "Unknown Product" && i.Price == 0m);
    }

    // ── Happy path — name and price filled from Catalog ─────────────────────────

    [Fact]
    public async Task Handle_CartExists_ReturnsCartDtoWithNameAndPrice()
    {
        using var ctx = BasketDbContextFactory.Create();
        var cart = new Cart(Customer);
        cart.AddItem(new CartItem(ProductA, Quantity.Of(3)));
        await ctx.Carts.AddAsync(cart);
        await ctx.SaveChangesAsync();

        var infos = new Dictionary<Guid, CatalogProductInfo>
        {
            [ProductA.Value] = ProductInfo("Super Widget", 19.99m)
        };
        var catalog = Substitute.For<ICatalogClient>();
        catalog.GetProductInfosByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
               .Returns(Result<IReadOnlyDictionary<Guid, CatalogProductInfo>>.Success(infos));

        var result = await CreateHandler(ctx, catalog).Handle(new GetCartQuery(Customer), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerId.Should().Be(Customer.Value);
        result.Value.Items.Should().ContainSingle(i =>
            i.ProductName == "Super Widget" && i.Price == 19.99m && i.Quantity == 3);
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
        catalog.GetProductInfosByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
               .ThrowsAsync(new HttpRequestException("network error"));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateHandler(ctx, catalog).Handle(new GetCartQuery(Customer), default));
    }
}
