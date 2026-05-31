using OrderSphere.Basket.Tests.Helpers;

namespace OrderSphere.Basket.Tests.Features;

public sealed class GetCartInternalQueryHandlerTests
{
    private static readonly CustomerId Customer = CustomerId.New();
    private static readonly ProductId ProductA  = ProductId.New();

    private static GetCartInternalQueryHandler CreateHandler(BasketDbContext ctx) =>
        new(ctx);

    // ── Warenkorb nicht vorhanden → 404 ────────────────────────────────────────

    [Fact]
    public async Task Handle_CartNotFound_ReturnsCartNotFoundError()
    {
        using var ctx = BasketDbContextFactory.Create();

        var result = await CreateHandler(ctx).Handle(new GetCartInternalQuery(Customer), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CartErrors.CartNotFoundError);
    }

    // ── Warenkorb vorhanden → Items ohne Preis und Name ────────────────────────

    [Fact]
    public async Task Handle_CartExists_ReturnsCartDtoWithEmptyNameAndZeroPrice()
    {
        using var ctx = BasketDbContextFactory.Create();
        var cart = new Cart(Customer);
        cart.AddItem(new CartItem(ProductA, Quantity.Of(5)));
        await ctx.Carts.AddAsync(cart);
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(new GetCartInternalQuery(Customer), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerId.Should().Be(Customer.Value);
        result.Value.Items.Should().ContainSingle(i =>
            i.ProductId == ProductA.Value && i.Price == 0m && i.ProductName == string.Empty && i.Quantity == 5);
    }
}
