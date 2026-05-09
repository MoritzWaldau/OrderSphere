using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Enums;
using OrderSphere.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderSphere.Domain.Entities;

public class Order: AuditableEntity
{
    public Guid CustomerId { get; private set; }
    public Address ShippingAddress { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Created;
    public PaymentMethod PaymentMethod { get; private set; }
    public string? TrackingNumber { get; private set; }
    public Guid CorrelationId { get; private set; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items;

    private Order() { }

    public Order(
        Guid customerId,
        Address shippingAddress,
        PaymentMethod paymentMethod,
        IEnumerable<OrderItem> items,
        Guid correlationId)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        ShippingAddress = shippingAddress;
        PaymentMethod = paymentMethod;
        _items.AddRange(items);
        Status = OrderStatus.Created;
        CorrelationId = correlationId;
    }

    public void Confirm(string trackingNumber)
    {
        TrackingNumber = trackingNumber;
        Status = OrderStatus.Paid;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkShipped()
    {
        if (Status is not OrderStatus.Paid)
            throw new InvalidOperationException(
                $"Order can only be marked as shipped when status is Paid (current: {Status}).");

        Status = OrderStatus.Shipped;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkDelivered()
    {
        if (Status is not OrderStatus.Shipped)
            throw new InvalidOperationException(
                $"Order can only be marked as delivered when status is Shipped (current: {Status}).");

        Status = OrderStatus.Delivered;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Delivered or OrderStatus.Cancelled)
            throw new InvalidOperationException(
                $"Order in status {Status} cannot be cancelled.");

        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}
