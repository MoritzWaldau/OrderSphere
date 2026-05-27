using FluentAssertions;
using OrderSphere.Basket.Domain.DomainEvents;
using OrderSphere.Basket.Domain.Entities;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.BuildingBlocks.ValueObjects;
using Xunit;

namespace OrderSphere.Domain.Tests.Aggregates;

public sealed class CartTests
{
    private static readonly CustomerId Customer = CustomerId.New();
    private static readonly ProductId Product1 = ProductId.New();
    private static readonly ProductId Product2 = ProductId.New();

    private static Cart CreateCart() => new(Customer);

    private static CartItem Item(ProductId productId, int qty = 1)
        => new(productId, Quantity.Of(qty));

    // ── Construction ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsCustomerId()
    {
        var cart = CreateCart();

        cart.CustomerId.Should().Be(Customer);
    }

    [Fact]
    public void Constructor_ItemsEmpty()
    {
        var cart = CreateCart();

        cart.Items.Should().BeEmpty();
    }

    // ── AddItem — new product ────────────────────────────────────────────────────

    [Fact]
    public void AddItem_NewProduct_AddsToItemsList()
    {
        var cart = CreateCart();

        cart.AddItem(Item(Product1));

        cart.Items.Should().ContainSingle(x => x.ProductId == Product1);
    }

    [Fact]
    public void AddItem_NewProduct_RaisesCartItemAddedDomainEvent()
    {
        var cart = CreateCart();

        cart.AddItem(Item(Product1, qty: 3));

        var events = cart.PopDomainEvents();
        events.Should().ContainSingle()
            .Which.Should().BeOfType<CartItemAddedDomainEvent>()
            .Which.Quantity.Value.Should().Be(3);
    }

    // ── AddItem — existing product (increase) ───────────────────────────────────

    [Fact]
    public void AddItem_ExistingProduct_IncreasesQuantity()
    {
        var cart = CreateCart();
        cart.AddItem(Item(Product1, qty: 2));
        cart.PopDomainEvents();

        cart.AddItem(Item(Product1, qty: 3));

        cart.Items.Should().ContainSingle(x => x.ProductId == Product1)
            .Which.Quantity.Value.Should().Be(5);
    }

    [Fact]
    public void AddItem_ExistingProduct_DoesNotAddDuplicateItem()
    {
        var cart = CreateCart();
        cart.AddItem(Item(Product1));
        cart.PopDomainEvents();

        cart.AddItem(Item(Product1));

        cart.Items.Should().ContainSingle(x => x.ProductId == Product1);
    }

    [Fact]
    public void AddItem_TwoDifferentProducts_BothPresent()
    {
        var cart = CreateCart();
        cart.AddItem(Item(Product1));
        cart.AddItem(Item(Product2));

        cart.Items.Should().HaveCount(2);
    }

    // ── Domain event payload ─────────────────────────────────────────────────────

    [Fact]
    public void AddItem_DomainEvent_ContainsCartAndProductId()
    {
        var cart = CreateCart();
        cart.AddItem(Item(Product1, qty: 2));

        var @event = cart.PopDomainEvents().OfType<CartItemAddedDomainEvent>().Single();

        @event.CartId.Should().Be(cart.Id);
        @event.ProductId.Should().Be(Product1);
    }
}
