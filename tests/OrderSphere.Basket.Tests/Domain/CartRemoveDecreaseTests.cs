using OrderSphere.Basket.Domain.DomainEvents;

namespace OrderSphere.Basket.Tests.Domain;

/// <summary>
/// Tests for <see cref="Cart.RemoveItem"/> and <see cref="Cart.DecreaseItem"/>.
/// AddItem scenarios are already covered in OrderSphere.Domain.Tests.
/// </summary>
public sealed class CartRemoveDecreaseTests
{
    private static readonly CustomerId Customer = CustomerId.New();
    private static readonly ProductId Product1 = ProductId.New();
    private static readonly ProductId Product2 = ProductId.New();

    private static Cart CreateCartWithItems(params (ProductId productId, int qty)[] items)
    {
        var cart = new Cart(Customer);
        foreach (var (pid, qty) in items)
            cart.AddItem(new CartItem(pid, Quantity.Of(qty)));
        cart.PopDomainEvents(); // drain add events
        return cart;
    }


    [Fact]
    public void RemoveItem_ExistingProduct_RemovesItemFromList()
    {
        var cart = CreateCartWithItems((Product1, 2));

        var result = cart.RemoveItem(Product1);

        result.IsSuccess.Should().BeTrue();
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void RemoveItem_ExistingProduct_RaisesCartItemRemovedDomainEvent()
    {
        var cart = CreateCartWithItems((Product1, 2));

        cart.RemoveItem(Product1);

        var events = cart.PopDomainEvents();
        events.Should().ContainSingle()
            .Which.Should().BeOfType<CartItemRemovedDomainEvent>()
            .Which.ProductId.Should().Be(Product1);
    }

    [Fact]
    public void RemoveItem_NotExistingProduct_ReturnsFailure()
    {
        var cart = CreateCartWithItems((Product1, 2));

        var result = cart.RemoveItem(Product2);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CartErrors.ItemNotFoundError);
    }

    [Fact]
    public void RemoveItem_NotExistingProduct_DoesNotRaiseEvent()
    {
        var cart = CreateCartWithItems((Product1, 2));

        cart.RemoveItem(Product2);

        cart.PopDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public void RemoveItem_OneOfTwoProducts_LeavesOtherProduct()
    {
        var cart = CreateCartWithItems((Product1, 1), (Product2, 3));

        cart.RemoveItem(Product1);

        cart.Items.Should().ContainSingle(x => x.ProductId == Product2);
    }


    [Fact]
    public void DecreaseItem_ExistingProduct_DecreasesQuantity()
    {
        var cart = CreateCartWithItems((Product1, 3));

        var result = cart.DecreaseItem(Product1);

        result.IsSuccess.Should().BeTrue();
        cart.Items.Should().ContainSingle(x => x.ProductId == Product1)
            .Which.Quantity.Value.Should().Be(2);
    }

    [Fact]
    public void DecreaseItem_QuantityReachesZero_RemovesItemFromCart()
    {
        var cart = CreateCartWithItems((Product1, 1));

        var result = cart.DecreaseItem(Product1);

        result.IsSuccess.Should().BeTrue();
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void DecreaseItem_RaisesCartItemDecreasedDomainEvent()
    {
        var cart = CreateCartWithItems((Product1, 2));

        cart.DecreaseItem(Product1);

        var events = cart.PopDomainEvents();
        events.Should().ContainSingle()
            .Which.Should().BeOfType<CartItemDecreasedDomainEvent>()
            .Which.ProductId.Should().Be(Product1);
    }

    [Fact]
    public void DecreaseItem_NotExistingProduct_ReturnsFailure()
    {
        var cart = CreateCartWithItems((Product1, 2));

        var result = cart.DecreaseItem(Product2);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CartErrors.ItemNotFoundError);
    }

    [Fact]
    public void DecreaseItem_NotExistingProduct_DoesNotRaiseEvent()
    {
        var cart = CreateCartWithItems((Product1, 2));

        cart.DecreaseItem(Product2);

        cart.PopDomainEvents().Should().BeEmpty();
    }
}
