using OrderSphere.Domain.Abstraction;

namespace OrderSphere.Ordering.Domain.Entities;

public sealed class Cart(Guid customerId) : AuditableEntity
{
    public Guid CustomerId { get; private set; } = customerId;
    public List<CartItem> Items { get; private set; } = [];

    public void AddItem(CartItem item)
    {
        var existing = Items.FirstOrDefault(x => x.ProductId == item.ProductId);

        if (existing is not null)
            existing.Increase(item.Quantity);
        else
            Items.Add(item);
    }
}
