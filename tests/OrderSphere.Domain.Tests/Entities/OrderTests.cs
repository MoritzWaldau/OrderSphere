using OrderSphere.Domain.Entities;
using OrderSphere.Domain.Enums;
using OrderSphere.Domain.ValueObjects;

namespace OrderSphere.Domain.Tests.Entities;

public class OrderTests
{
    private static Order CreateOrder(OrderStatus? targetStatus = null)
    {
        var address = new Address("Moritz", "Waldau", "Schwarmstedter Str. 2", "Essel", "29690", "Germany");
        var items = new List<OrderItem> { new(Guid.NewGuid(), 2, 19.99m) };
        var order = new Order(Guid.NewGuid(), address, PaymentMethod.Invoice, items, Guid.NewGuid());

        if (targetStatus is null) return order;

        // Walk the lifecycle to reach the requested status
        if (targetStatus is OrderStatus.Paid or OrderStatus.Shipped or OrderStatus.Delivered)
            order.Confirm("OS-2026-ABCDEF12");
        if (targetStatus is OrderStatus.Shipped or OrderStatus.Delivered)
            order.MarkShipped();
        if (targetStatus is OrderStatus.Delivered)
            order.MarkDelivered();
        if (targetStatus is OrderStatus.Cancelled)
            order.Cancel();

        return order;
    }

    [Fact]
    public void Constructor_SetsCreatedStatusAndCorrelationId()
    {
        var correlationId = Guid.NewGuid();
        var address = new Address("Moritz", "Waldau", "Street", "City", "12345", "DE");

        var order = new Order(Guid.NewGuid(), address, PaymentMethod.Invoice,
            [new OrderItem(Guid.NewGuid(), 1, 10m)], correlationId);

        order.Status.Should().Be(OrderStatus.Created);
        order.CorrelationId.Should().Be(correlationId);
        order.TrackingNumber.Should().BeNull();
        order.Items.Should().HaveCount(1);
    }

    [Fact]
    public void Confirm_SetsTrackingAndPaidStatus()
    {
        var order = CreateOrder();

        order.Confirm("OS-2026-12345678");

        order.Status.Should().Be(OrderStatus.Paid);
        order.TrackingNumber.Should().Be("OS-2026-12345678");
        order.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkShipped_FromPaid_TransitionsToShipped()
    {
        var order = CreateOrder(OrderStatus.Paid);

        order.MarkShipped();

        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Theory]
    [InlineData(OrderStatus.Created)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void MarkShipped_FromInvalidStatus_Throws(OrderStatus initialStatus)
    {
        var order = CreateOrder(initialStatus);

        var act = () => order.MarkShipped();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkDelivered_FromShipped_TransitionsToDelivered()
    {
        var order = CreateOrder(OrderStatus.Shipped);

        order.MarkDelivered();

        order.Status.Should().Be(OrderStatus.Delivered);
    }

    [Theory]
    [InlineData(OrderStatus.Created)]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Delivered)]
    public void MarkDelivered_FromInvalidStatus_Throws(OrderStatus initialStatus)
    {
        var order = CreateOrder(initialStatus);

        var act = () => order.MarkDelivered();

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(OrderStatus.Created)]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Shipped)]
    public void Cancel_FromAllowedStatus_Succeeds(OrderStatus initialStatus)
    {
        var order = CreateOrder(initialStatus);

        order.Cancel();

        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Theory]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void Cancel_FromTerminalStatus_Throws(OrderStatus initialStatus)
    {
        var order = CreateOrder(initialStatus);

        var act = () => order.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }
}
