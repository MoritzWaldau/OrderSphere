using OrderSphere.Basket.Domain.DomainEvents;
using OrderSphere.Basket.Domain.Errors;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.BuildingBlocks.ValueObjects;

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
        {
            item.CartId = Id;
            Items.Add(item);
        }

        RaiseDomainEvent(new CartItemAddedDomainEvent(Id, item.ProductId, item.Quantity));
    }

    /// <summary>
    /// Removes the item with the given product ID entirely.
    /// Raises <see cref="CartItemRemovedDomainEvent"/>.
    /// Returns <see cref="Result.Failure"/> if the item does not exist in this cart.
    /// </summary>
    public Result RemoveItem(ProductId productId)
    {
        var item = Items.FirstOrDefault(x => x.ProductId == productId);
        if (item is null)
            return Result.Failure(CartErrors.ItemNotFoundError);

        Items.Remove(item);
        RaiseDomainEvent(new CartItemRemovedDomainEvent(Id, productId));
        return Result.Success();
    }

    /// <summary>
    /// Decreases the quantity of the item with the given product ID by one.
    /// Removes the item automatically when the quantity reaches zero.
    /// Raises <see cref="CartItemDecreasedDomainEvent"/>.
    /// Returns <see cref="Result.Failure"/> if the item does not exist in this cart.
    /// </summary>
    public Result DecreaseItem(ProductId productId)
    {
        var item = Items.FirstOrDefault(x => x.ProductId == productId);
        if (item is null)
            return Result.Failure(CartErrors.ItemNotFoundError);

        item.Decrease();

        if (item.Quantity.Value == 0)
            Items.Remove(item);

        RaiseDomainEvent(new CartItemDecreasedDomainEvent(Id, productId, item.Quantity));
        return Result.Success();
    }
}
