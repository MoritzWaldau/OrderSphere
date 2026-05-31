using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Payment.Domain.DomainEvents;

public sealed record PaymentFailedDomainEvent(
    PaymentId PaymentId,
    OrderId OrderId,
    string Reason) : IDomainEvent;
