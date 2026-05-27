using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Ordering.Domain.DomainEvents;

public sealed record OrderShippedDomainEvent(OrderId OrderId) : IDomainEvent;
