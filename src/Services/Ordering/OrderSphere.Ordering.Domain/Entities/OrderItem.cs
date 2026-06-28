using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.BuildingBlocks.ValueObjects;

namespace OrderSphere.Ordering.Domain.Entities;

public sealed class OrderItem : AuditableEntity<OrderItemId>
{
    public ProductId ProductId { get; private set; }
    public string ProductName { get; private set; }
    public Quantity Quantity { get; private set; }
    public Money Price { get; private set; } = null!;

    /// <summary>Category of the product at order-capture time; null for orders placed before B3.</summary>
    public Guid? CategoryId { get; private set; }

    // Parameterless constructor for EF Core materialisation.
    private OrderItem()
    {
        ProductId = ProductId.Empty;
        ProductName = string.Empty;
    }

    public OrderItem(ProductId productId, string productName, Quantity quantity, Money price, Guid? categoryId = null)
    {
        Id = OrderItemId.New();
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        Price = price;
        CategoryId = categoryId;
    }
}
