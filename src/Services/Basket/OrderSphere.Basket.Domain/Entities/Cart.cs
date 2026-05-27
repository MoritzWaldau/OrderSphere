using OrderSphere.Basket.Domain.DomainEvents;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Basket.Domain.Entities;

public sealed class Cart : AuditableEntity<CartId>, IAggregateRoot
{
    public CustomerId CustomerId { get; private set; }
    public List<CartItem> Items { get; private set; } = [];

    // Parameterless constructor for EF Core materialisation.
    private Cart()
    {
        CustomerId = CustomerId.Empty;
    }

    public Cart(CustomerId customerId)
    {
        Id = CartId.New();
        CustomerId = customerId;
    }

    public void AddItem(CartItem item)
    {
        var existing = Items.FirstOrDefault(x => x.ProductId == item.ProductId);

        if (existing is not null)
            existing.Increase(item.Quantity);
        else
            Items.Add(item);

        RaiseDomainEvent(new CartItemAddedDomainEvent(Id, item.ProductId, item.Quantity));
    }
}
