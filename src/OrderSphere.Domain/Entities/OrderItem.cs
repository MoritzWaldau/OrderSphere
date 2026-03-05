namespace OrderSphere.Domain.Entities;

public sealed class OrderItem(Guid productId, int quantity, decimal price)
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; } = productId;
    public int Quantity { get; private set; } = quantity;
    public decimal Price { get; private set; } = price;
}
