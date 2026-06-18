using FluentAssertions;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.BuildingBlocks.ValueObjects;
using OrderSphere.Ordering.Domain.DomainEvents;
using OrderSphere.Ordering.Domain.Entities;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.ValueObjects;
using Xunit;

namespace OrderSphere.Domain.Tests.Aggregates;

public sealed class OrderTests
{
    // ── Shared test data ────────────────────────────────────────────────────────

    private static readonly CustomerId Customer = CustomerId.New();
    private static readonly Address Addr = new("Max", "Muster", "Hauptstr. 1", "Berlin", "10115", "DE");
    private static readonly Guid Correlation = Guid.NewGuid();

    private static Order CreateOrder(IEnumerable<OrderItem>? items = null)
    {
        var defaultItems = items ?? [new OrderItem(ProductId.New(), "Widget", Quantity.Of(2), Money.Of(5m))];
        return new Order(Customer, Addr, PaymentMethod.CreditCard, defaultItems, Correlation);
    }

    // ── Construction ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ValidInputs_SetsStatusCreated()
    {
        var order = CreateOrder();

        order.Status.Should().Be(OrderStatus.Created);
    }

    [Fact]
    public void Constructor_AppendsCreatedStatusHistory()
    {
        var order = CreateOrder();

        order.StatusHistory.Should().ContainSingle()
            .Which.Status.Should().Be(OrderStatus.Created);
    }

    [Fact]
    public void StatusTransitions_AppendTimelineEntries_InOrder()
    {
        var order = CreateOrder();
        order.Confirm("TRACK-1");
        order.MarkShipped();
        order.MarkDelivered();

        order.StatusHistory.Select(h => h.Status).Should()
            .Equal(OrderStatus.Created, OrderStatus.Paid, OrderStatus.Shipped, OrderStatus.Delivered);
    }

    [Fact]
    public void SetShippingCost_SetsValue()
    {
        var order = CreateOrder();

        order.SetShippingCost(4.99m);

        order.ShippingCost.Should().Be(4.99m);
    }

    [Fact]
    public void Constructor_EmptyItems_Throws()
    {
        var act = () => CreateOrder([]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_RaisesOrderCreatedDomainEvent()
    {
        var order = CreateOrder();

        var events = order.PopDomainEvents();

        events.Should().ContainSingle()
            .Which.Should().BeOfType<OrderCreatedDomainEvent>();
    }

    [Fact]
    public void Constructor_CreatedEvent_HasCorrectCorrelationId()
    {
        var order = CreateOrder();
        var @event = order.PopDomainEvents().OfType<OrderCreatedDomainEvent>().Single();

        @event.CorrelationId.Should().Be(Correlation);
        @event.CustomerId.Should().Be(Customer);
    }

    // ── Confirm ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Confirm_SetsStatusPaid()
    {
        var order = CreateOrder();
        order.PopDomainEvents(); // drain constructor event

        order.Confirm("TRACK-001");

        order.Status.Should().Be(OrderStatus.Paid);
        order.TrackingNumber.Should().Be("TRACK-001");
    }

    [Fact]
    public void Confirm_RaisesOrderConfirmedDomainEvent()
    {
        var order = CreateOrder();
        order.PopDomainEvents();

        order.Confirm("TRACK-001");

        var events = order.PopDomainEvents();
        events.Should().ContainSingle().Which.Should().BeOfType<OrderConfirmedDomainEvent>();
    }

    // ── MarkShipped ─────────────────────────────────────────────────────────────

    [Fact]
    public void MarkShipped_FromPaid_SetsStatusShipped()
    {
        var order = CreateOrder();
        order.PopDomainEvents();
        order.Confirm("T");

        order.MarkShipped();

        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void MarkShipped_FromCreated_Throws()
    {
        var order = CreateOrder();

        var act = () => order.MarkShipped();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkShipped_RaisesOrderShippedDomainEvent()
    {
        var order = CreateOrder();
        order.PopDomainEvents();
        order.Confirm("T");
        order.PopDomainEvents();

        order.MarkShipped();

        order.PopDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<OrderShippedDomainEvent>();
    }

    // ── MarkDelivered ───────────────────────────────────────────────────────────

    [Fact]
    public void MarkDelivered_FromShipped_SetsStatusDelivered()
    {
        var order = CreateOrder();
        order.PopDomainEvents();
        order.Confirm("T");
        order.MarkShipped();

        order.MarkDelivered();

        order.Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public void MarkDelivered_FromPaid_Throws()
    {
        var order = CreateOrder();
        order.Confirm("T");

        var act = () => order.MarkDelivered();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkDelivered_RaisesOrderDeliveredDomainEvent()
    {
        var order = CreateOrder();
        order.PopDomainEvents();
        order.Confirm("T");
        order.MarkShipped();
        order.PopDomainEvents();

        order.MarkDelivered();

        order.PopDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<OrderDeliveredDomainEvent>();
    }

    // ── Cancel ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_FromCreated_SetsStatusCancelled()
    {
        var order = CreateOrder();

        order.Cancel();

        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromDelivered_Throws()
    {
        var order = CreateOrder();
        order.Confirm("T");
        order.MarkShipped();
        order.MarkDelivered();

        var act = () => order.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Throws()
    {
        var order = CreateOrder();
        order.Cancel();

        var act = () => order.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_RaisesOrderCancelledDomainEvent()
    {
        var order = CreateOrder();
        order.PopDomainEvents();

        order.Cancel();

        order.PopDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<OrderCancelledDomainEvent>();
    }

    // ── PopDomainEvents ─────────────────────────────────────────────────────────

    [Fact]
    public void PopDomainEvents_CalledTwice_SecondCallReturnsEmpty()
    {
        var order = CreateOrder();
        order.PopDomainEvents(); // drain

        var second = order.PopDomainEvents();

        second.Should().BeEmpty();
    }
}
