using OrderSphere.Domain.Abstraction;

namespace OrderSphere.Ordering.Domain.Entities;

public sealed class OrderItem(Guid productId, string productName, int quantity, decimal price) : AuditableEntity
{
    public Guid ProductId { get; private set; } = productId;
    public string ProductName { get; private set; } = productName;
    public int Quantity { get; private set; } = quantity;
    public decimal Price { get; private set; } = price;
}
