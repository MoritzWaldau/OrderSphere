using OrderSphere.Domain.Enums;

namespace OrderSphere.Domain.Entities;

public class Order(Guid customerId)
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CustomerId { get; private set; } = customerId;
    public OrderStatus Status { get; private set; } = OrderStatus.Created;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public List<OrderItem> Items { get; private set; } = [];

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
