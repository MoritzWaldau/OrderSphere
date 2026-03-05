using System;
namespace OrderSphere.Domain.Entities;

public sealed class Cart(Guid userId)
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; } = userId;
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
