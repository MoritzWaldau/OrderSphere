using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.BuildingBlocks.ValueObjects;

namespace OrderSphere.Basket.Domain.Entities;

public sealed class CartItem : AuditableEntity<CartItemId>
{
    public ProductId ProductId { get; private set; }
    public Quantity Quantity { get; private set; }
    public CartId CartId { get; set; }

    // Parameterless constructor for EF Core materialisation.
    private CartItem()
    {
        ProductId = ProductId.Empty;
        CartId = CartId.Empty;
    }

    public CartItem(ProductId productId, Quantity quantity)
    {
        Id = CartItemId.New();
        ProductId = productId;
        Quantity = quantity;
    }

    public void Increase(int amount) => Quantity += amount;
    public void Decrease(int amount = 1) => Quantity -= amount;
}
