using OrderSphere.Basket.Tests.Helpers;

namespace OrderSphere.Basket.Tests.Features;

public sealed class RemoveFromCartCommandHandlerTests
{
    private static readonly CustomerId Customer = CustomerId.New();
    private static readonly ProductId  Product1 = ProductId.New();
    private static readonly ProductId  Product2 = ProductId.New();

    private static RemoveFromCartCommandHandler CreateHandler(BasketDbContext ctx) =>
        new(ctx);

    // ── Cart not found ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CartNotFound_ReturnsCartNotFoundError()
    {
        using var ctx = BasketDbContextFactory.Create();

        var result = await CreateHandler(ctx).Handle(new(Customer, Product1), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CartErrors.CartNotFoundError);
    }

    // ── Item not found ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ItemNotInCart_ReturnsItemNotFoundError()
    {
        using var ctx = BasketDbContextFactory.Create();
        await SeedCart(ctx, (Product1, 2));

        var result = await CreateHandler(ctx).Handle(new(Customer, Product2), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CartErrors.ItemNotFoundError);
    }

    // ── Happy path — item removed, cart still has items ─────────────────────────

    [Fact]
    public async Task Handle_RemovingOneOfTwoItems_CartStillExists()
    {
        using var ctx = BasketDbContextFactory.Create();
        await SeedCart(ctx, (Product1, 1), (Product2, 3));

        var result = await CreateHandler(ctx).Handle(new(Customer, Product1), default);

        result.IsSuccess.Should().BeTrue();
        var cart = await ctx.Carts.Include(c => c.Items)
                                   .FirstOrDefaultAsync(c => c.CustomerId == Customer);
        cart.Should().NotBeNull();
        cart!.Items.Should().ContainSingle(i => i.ProductId == Product2);
    }

    // ── Happy path — last item removed, cart deleted ─────────────────────────────

    [Fact]
    public async Task Handle_RemovingLastItem_CartIsDeleted()
    {
        using var ctx = BasketDbContextFactory.Create();
        await SeedCart(ctx, (Product1, 2));

        var result = await CreateHandler(ctx).Handle(new(Customer, Product1), default);

        result.IsSuccess.Should().BeTrue();
        var cart = await ctx.Carts.FirstOrDefaultAsync(c => c.CustomerId == Customer);
        cart.Should().BeNull();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static async Task SeedCart(BasketDbContext ctx, params (ProductId pid, int qty)[] items)
    {
        var cart = new Cart(Customer);
        foreach (var (pid, qty) in items)
            cart.AddItem(new CartItem(pid, Quantity.Of(qty)));
        await ctx.Carts.AddAsync(cart);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
    }
}
