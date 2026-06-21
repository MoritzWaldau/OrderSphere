using OrderSphere.Basket.Tests.Helpers;

namespace OrderSphere.Basket.Tests.Features;

public sealed class DecreaseCartItemQuantityCommandHandlerTests
{
    private static readonly CustomerId Customer = CustomerId.New();
    private static readonly ProductId Product1 = ProductId.New();
    private static readonly ProductId Product2 = ProductId.New();

    private static DecreaseCartItemQuantityCommandHandler CreateHandler(BasketDbContext ctx) =>
        new(ctx);


    [Fact]
    public async Task Handle_CartNotFound_ReturnsCartNotFoundError()
    {
        using var ctx = BasketDbContextFactory.Create();

        var result = await CreateHandler(ctx).Handle(new(Customer, Product1), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CartErrors.CartNotFoundError);
    }


    [Fact]
    public async Task Handle_ItemNotInCart_ReturnsItemNotFoundError()
    {
        using var ctx = BasketDbContextFactory.Create();
        await SeedCart(ctx, (Product1, 2));

        var result = await CreateHandler(ctx).Handle(new(Customer, Product2), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CartErrors.ItemNotFoundError);
    }


    [Fact]
    public async Task Handle_ItemWithMultipleQty_DecreasesQuantityByOne()
    {
        using var ctx = BasketDbContextFactory.Create();
        await SeedCart(ctx, (Product1, 3));

        var result = await CreateHandler(ctx).Handle(new(Customer, Product1), default);

        result.IsSuccess.Should().BeTrue();
        ctx.ChangeTracker.Clear();
        var cart = await ctx.Carts.Include(c => c.Items)
                                   .FirstOrDefaultAsync(c => c.CustomerId == Customer);
        cart!.Items.Single(i => i.ProductId == Product1).Quantity.Value.Should().Be(2);
    }


    [Fact]
    public async Task Handle_ItemQtyOne_ItemRemovedAndCartDeleted()
    {
        using var ctx = BasketDbContextFactory.Create();
        await SeedCart(ctx, (Product1, 1));

        var result = await CreateHandler(ctx).Handle(new(Customer, Product1), default);

        result.IsSuccess.Should().BeTrue();
        ctx.ChangeTracker.Clear();
        var cart = await ctx.Carts.FirstOrDefaultAsync(c => c.CustomerId == Customer);
        cart.Should().BeNull();
    }


    [Fact]
    public async Task Handle_ItemQtyOne_OtherItemStillInCart()
    {
        using var ctx = BasketDbContextFactory.Create();
        await SeedCart(ctx, (Product1, 1), (Product2, 5));

        var result = await CreateHandler(ctx).Handle(new(Customer, Product1), default);

        result.IsSuccess.Should().BeTrue();
        ctx.ChangeTracker.Clear();
        var cart = await ctx.Carts.Include(c => c.Items)
                                   .FirstOrDefaultAsync(c => c.CustomerId == Customer);
        cart.Should().NotBeNull();
        cart!.Items.Should().ContainSingle(i => i.ProductId == Product2);
    }


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
