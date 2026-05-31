using FluentAssertions;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.BuildingBlocks.ValueObjects;
using OrderSphere.Catalog.Domain.DomainEvents;
using OrderSphere.Catalog.Domain.Entities;
using Xunit;

namespace OrderSphere.Domain.Tests.Aggregates;

public sealed class ProductTests
{
    private static readonly CategoryId DefaultCategory = CategoryId.New();

    private static Product CreateProduct(int stock = 10)
        => new("Widget", "A test widget", Money.Of(9.99m), stock, DefaultCategory, "WDG-001");

    // ── RemoveFromStock ─────────────────────────────────────────────────────────

    [Fact]
    public void RemoveFromStock_SufficientStock_DecreasesStock()
    {
        var product = CreateProduct(stock: 10);

        var result = product.RemoveFromStock(3);

        result.IsSuccess.Should().BeTrue();
        product.Stock.Should().Be(7);
    }

    [Fact]
    public void RemoveFromStock_ExactStock_ReducesToZero()
    {
        var product = CreateProduct(stock: 5);

        var result = product.RemoveFromStock(5);

        result.IsSuccess.Should().BeTrue();
        product.Stock.Should().Be(0);
    }

    [Fact]
    public void RemoveFromStock_InsufficientStock_ReturnsFailure()
    {
        var product = CreateProduct(stock: 3);

        var result = product.RemoveFromStock(5);

        result.IsFailure.Should().BeTrue();
        product.Stock.Should().Be(3); // unchanged
    }

    [Fact]
    public void RemoveFromStock_RaisesProductStockDecreasedDomainEvent()
    {
        var product = CreateProduct(stock: 10);

        product.RemoveFromStock(4);

        var events = product.PopDomainEvents();
        events.Should().ContainSingle()
            .Which.Should().BeOfType<ProductStockDecreasedDomainEvent>()
            .Which.Quantity.Should().Be(4);
    }

    [Fact]
    public void RemoveFromStock_Failure_DoesNotRaiseDomainEvent()
    {
        var product = CreateProduct(stock: 2);

        product.RemoveFromStock(5);

        product.PopDomainEvents().Should().BeEmpty();
    }

    // ── AddToStock ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddToStock_IncreasesStock()
    {
        var product = CreateProduct(stock: 5);

        product.AddToStock(10);

        product.Stock.Should().Be(15);
    }

    // ── Activate / Deactivate ───────────────────────────────────────────────────

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var product = CreateProduct();
        product.Deactivate();
        product.PopDomainEvents();

        product.Activate();

        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_RaisesProductActivatedDomainEvent()
    {
        var product = CreateProduct();
        product.Deactivate();
        product.PopDomainEvents();

        product.Activate();

        product.PopDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<ProductActivatedDomainEvent>();
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var product = CreateProduct();

        product.Deactivate();

        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_RaisesProductDeactivatedDomainEvent()
    {
        var product = CreateProduct();

        product.Deactivate();

        product.PopDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<ProductDeactivatedDomainEvent>();
    }

    // ── UpdateDetails ───────────────────────────────────────────────────────────

    [Fact]
    public void UpdateDetails_ChangesName()
    {
        var product = CreateProduct();

        product.UpdateDetails("New Name", "desc", Money.Of(15m), 20, DefaultCategory, "NEW-001");

        product.Name.Should().Be("New Name");
        product.Price.Should().Be(Money.Of(15m));
    }
}
