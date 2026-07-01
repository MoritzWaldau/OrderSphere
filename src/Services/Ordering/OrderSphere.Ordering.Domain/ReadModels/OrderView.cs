using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Domain.Entities;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.ValueObjects;

namespace OrderSphere.Ordering.Domain.ReadModels;

/// <summary>
/// Read-side projection of the order event stream. Maps to the <c>orders</c>,
/// <c>order_items</c>, and <c>order_status_history</c> tables that all order queries read from.
/// Updated synchronously, in the same transaction that appends events to the stream, so reads
/// stay consistent with the write side. Mutations are driven exclusively by the projector and
/// mirror the aggregate's transitions one-for-one.
/// </summary>
public sealed class OrderView : AuditableEntity<OrderId>
{
    public CustomerId CustomerId { get; private set; }
    public Address ShippingAddress { get; private set; } = null!;
    public OrderStatus Status { get; private set; } = OrderStatus.Created;
    public PaymentMethod PaymentMethod { get; private set; }
    public string? TrackingNumber { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string? CouponCode { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal ShippingCost { get; private set; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items;

    private readonly List<OrderStatusHistory> _statusHistory = [];
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory;

    private OrderView()
    {
        CustomerId = CustomerId.Empty;
    }

    /// <summary>Projects an <c>OrderCreated</c> event into a fresh read row.</summary>
    public static OrderView Create(
        OrderId id,
        CustomerId customerId,
        Address shippingAddress,
        PaymentMethod paymentMethod,
        Guid correlationId,
        IEnumerable<OrderItem> items,
        DateTime occurredAt)
    {
        var view = new OrderView
        {
            Id = id,
            CustomerId = customerId,
            ShippingAddress = shippingAddress,
            PaymentMethod = paymentMethod,
            CorrelationId = correlationId,
            Status = OrderStatus.Created
        };
        view._items.AddRange(items);
        view.AppendStatus(OrderStatus.Created, occurredAt);
        return view;
    }

    public void ApplyDiscount(string couponCode, decimal amount)
    {
        CouponCode = couponCode;
        DiscountAmount = amount;
    }

    public void SetShippingCost(decimal amount) => ShippingCost = amount;

    public void Confirm(string trackingNumber, DateTime occurredAt)
    {
        TrackingNumber = trackingNumber;
        Status = OrderStatus.Paid;
        AppendStatus(OrderStatus.Paid, occurredAt);
    }

    public void MarkShipped(DateTime occurredAt)
    {
        Status = OrderStatus.Shipped;
        AppendStatus(OrderStatus.Shipped, occurredAt);
    }

    public void MarkDelivered(DateTime occurredAt)
    {
        Status = OrderStatus.Delivered;
        AppendStatus(OrderStatus.Delivered, occurredAt);
    }

    public void Cancel(DateTime occurredAt)
    {
        Status = OrderStatus.Cancelled;
        AppendStatus(OrderStatus.Cancelled, occurredAt);
    }

    private void AppendStatus(OrderStatus status, DateTime occurredAt)
        => _statusHistory.Add(new OrderStatusHistory(status, occurredAt));

    /// <summary>
    /// D1 — GDPR right-to-erasure. Overwrites the shipping address on this read-model row.
    /// Only the projection is anonymized: the immutable <c>order_events</c> stream that this
    /// aggregate is sourced from is a deliberately scoped island (see docs/architecture.md) and
    /// is not rewritten here.
    /// </summary>
    public void AnonymizeShippingAddress()
        => ShippingAddress = Address.Erased(ShippingAddress.Country);
}
