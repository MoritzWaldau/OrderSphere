using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Catalog.Domain.DomainEvents;

public sealed record ProductStockDecreasedDomainEvent(
    ProductId ProductId,
    int Quantity) : IDomainEvent;
