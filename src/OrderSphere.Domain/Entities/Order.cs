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

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items;

    private Order() { }

    public Order(
        Guid customerId,
        Address shippingAddress,
        PaymentMethod paymentMethod,
        IEnumerable<OrderItem> items)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        ShippingAddress = shippingAddress;
        PaymentMethod = paymentMethod;
        _items.AddRange(items);
        Status = OrderStatus.Created;
    }
}
