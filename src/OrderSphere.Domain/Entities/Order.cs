using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderSphere.Domain.Entities;

public class Order(Guid customerId) : AuditableEntity
{
    public Guid CustomerId { get; private set; } = customerId;
    public OrderStatus Status { get; private set; } = OrderStatus.Created;
    public PaymentMethod PaymentMethod { get; private set; }
    public ICollection<OrderItem> Items { get; private set; } = [];

    public void AddItem(Guid productId, int quantity, decimal price)
    {
        Items.Add(new OrderItem(productId, quantity, price));
    }

    public void MarkAsPaid()
    {
        Status = OrderStatus.Paid;
    }

    public void Ship()
    {
        Status = OrderStatus.Shipped;
    }
}
