using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Catalog.Domain.DomainEvents;

public sealed record BrandDeletedDomainEvent(BrandId BrandId) : IDomainEvent;
