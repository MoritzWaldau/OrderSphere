namespace OrderSphere.Domain.Entities;

public sealed class CartItem(Guid productId, int quantity)
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; } = productId;
    public int Quantity { get; private set; } = quantity;

    public void Increase(int amount)
    {
        Quantity += amount;
    }
}
