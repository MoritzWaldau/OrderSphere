using OrderSphere.Basket.Tests.Helpers;

namespace OrderSphere.Basket.Tests.Features;

public sealed class ClearCartCommandHandlerTests
{
    private static readonly CustomerId Customer = CustomerId.New();
    private static readonly ProductId ProductA = ProductId.New();

    private static ClearCartCommandHandler CreateHandler(BasketDbContext ctx) =>
        new(ctx);


    [Fact]
    public async Task Handle_CartNotFound_ReturnsCartNotFoundError()
    {
        using var ctx = BasketDbContextFactory.Create();

        var result = await CreateHandler(ctx).Handle(new ClearCartCommand(Customer), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CartErrors.CartNotFoundError);
    }


    [Fact]
    public async Task Handle_CartWithItems_RemovesAllItems()
    {
        using var ctx = BasketDbContextFactory.Create();
        var cart = new Cart(Customer);
        cart.AddItem(new CartItem(ProductA, Quantity.Of(3)));
        await ctx.Carts.AddAsync(cart);
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(new ClearCartCommand(Customer), default);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await ctx.Carts.Include(c => c.Items)
                                      .FirstOrDefaultAsync(c => c.CustomerId == Customer);
        reloaded!.Items.Should().BeEmpty();
    }
}
