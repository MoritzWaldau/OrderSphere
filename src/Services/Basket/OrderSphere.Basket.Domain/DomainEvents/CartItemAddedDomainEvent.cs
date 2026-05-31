using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.BuildingBlocks.ValueObjects;

namespace OrderSphere.Basket.Domain.DomainEvents;

public sealed record CartItemAddedDomainEvent(
    CartId CartId,
    ProductId ProductId,
    Quantity Quantity) : IDomainEvent;
