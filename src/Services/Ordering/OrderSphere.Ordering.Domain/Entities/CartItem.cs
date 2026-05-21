using OrderSphere.Domain.Abstraction;

namespace OrderSphere.Ordering.Domain.Entities;

public sealed class CartItem(Guid productId, int quantity) : AuditableEntity
{
    public Guid ProductId { get; private set; } = productId;
    public int Quantity { get; private set; } = quantity;
    public Guid CartId { get; set; }

    public void Increase(int amount) => Quantity += amount;

    public void Decrease(int amount = 1) => Quantity -= amount;
}
