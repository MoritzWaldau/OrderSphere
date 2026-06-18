using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Domain.DomainEvents;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.ValueObjects;

namespace OrderSphere.Ordering.Domain.Entities;

public class Order : AuditableEntity<OrderId>, IAggregateRoot
{
    public CustomerId CustomerId { get; private set; }
    public Address ShippingAddress { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Created;
    public PaymentMethod PaymentMethod { get; private set; }
    public string? TrackingNumber { get; private set; }
    public Guid CorrelationId { get; private set; }

    /// <summary>Applied coupon code, or null when no coupon was used.</summary>
    public string? CouponCode { get; private set; }

    /// <summary>Discount applied to the order subtotal, in EUR. Zero when no coupon was used.</summary>
    public decimal DiscountAmount { get; private set; }

    /// <summary>Shipping cost added to the order total, in EUR. Set once during order processing.</summary>
    public decimal ShippingCost { get; private set; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items;

    private readonly List<OrderStatusHistory> _statusHistory = [];
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory;

    private Order()
    {
        CustomerId = CustomerId.Empty;
        ShippingAddress = null!;
    }

    public Order(
        CustomerId customerId,
        Address shippingAddress,
        PaymentMethod paymentMethod,
        IEnumerable<OrderItem> items,
        Guid correlationId)
    {
        ArgumentNullException.ThrowIfNull(shippingAddress);

        var itemList = items?.ToList() ?? throw new ArgumentNullException(nameof(items));
        if (itemList.Count == 0)
            throw new ArgumentException("An order must contain at least one item.", nameof(items));

        Id = OrderId.New();
        CustomerId = customerId;
        ShippingAddress = shippingAddress;
        PaymentMethod = paymentMethod;
        _items.AddRange(itemList);
        Status = OrderStatus.Created;
        CorrelationId = correlationId;

        AppendStatus(OrderStatus.Created);
        RaiseDomainEvent(new OrderCreatedDomainEvent(Id, CustomerId, CorrelationId));
    }

    /// <summary>Records a redeemed coupon and its discount. Set once during order creation.</summary>
    public void ApplyDiscount(string couponCode, decimal amount)
    {
        CouponCode = couponCode;
        DiscountAmount = amount;
    }

    /// <summary>Records the calculated shipping cost. Set once during order processing.</summary>
    public void SetShippingCost(decimal amount) => ShippingCost = amount;

    public void Confirm(string trackingNumber)
    {
        TrackingNumber = trackingNumber;
        Status = OrderStatus.Paid;
        AppendStatus(OrderStatus.Paid);
        RaiseDomainEvent(new OrderConfirmedDomainEvent(Id, trackingNumber));
    }

    public void MarkShipped()
    {
        if (Status is not OrderStatus.Paid)
            throw new InvalidOperationException(
                $"Order can only be marked as shipped when status is Paid (current: {Status}).");

        Status = OrderStatus.Shipped;
        AppendStatus(OrderStatus.Shipped);
        RaiseDomainEvent(new OrderShippedDomainEvent(Id));
    }

    public void MarkDelivered()
    {
        if (Status is not OrderStatus.Shipped)
            throw new InvalidOperationException(
                $"Order can only be marked as delivered when status is Shipped (current: {Status}).");

        Status = OrderStatus.Delivered;
        AppendStatus(OrderStatus.Delivered);
        RaiseDomainEvent(new OrderDeliveredDomainEvent(Id));
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Delivered or OrderStatus.Cancelled)
            throw new InvalidOperationException(
                $"Order in status {Status} cannot be cancelled.");

        Status = OrderStatus.Cancelled;
        AppendStatus(OrderStatus.Cancelled);
        RaiseDomainEvent(new OrderCancelledDomainEvent(Id));
    }

    private void AppendStatus(OrderStatus status, string? note = null)
        => _statusHistory.Add(new OrderStatusHistory(status, DateTime.UtcNow, note));
}
