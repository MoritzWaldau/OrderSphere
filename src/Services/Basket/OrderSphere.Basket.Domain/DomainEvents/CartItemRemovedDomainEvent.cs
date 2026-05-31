using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Basket.Domain.DomainEvents;

public sealed record CartItemRemovedDomainEvent(
    CartId CartId,
    ProductId ProductId) : IDomainEvent;
