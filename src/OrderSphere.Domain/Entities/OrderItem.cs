using OrderSphere.BuildingBlocks.Abstraction;

namespace OrderSphere.Domain.Entities;

public sealed class OrderItem(Guid productId, int quantity, decimal price) : AuditableEntity
{
    public Guid ProductId { get; private set; } = productId;
    public int Quantity { get; private set; } = quantity;
    public decimal Price { get; private set; } = price;
}
