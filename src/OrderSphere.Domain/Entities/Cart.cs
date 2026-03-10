using OrderSphere.Domain.Abstraction;
namespace OrderSphere.Domain.Entities;

public sealed class Cart(Guid customerId) : AuditableEntity
{
    public Guid CustomerId { get; private set; } = customerId;
    public List<CartItem> Items { get; private set; } = [];

    public void AddItem(Guid productId, int quantity)
    {
        var existing = Items.FirstOrDefault(x => x.ProductId == productId);

        if (existing != null)
            existing.Increase(quantity);
        else
            Items.Add(new CartItem(productId, quantity));
    }
}
