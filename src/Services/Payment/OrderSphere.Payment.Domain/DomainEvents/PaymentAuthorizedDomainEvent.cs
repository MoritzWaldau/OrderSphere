using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Payment.Domain.DomainEvents;

public sealed record PaymentAuthorizedDomainEvent(
    PaymentId PaymentId,
    OrderId OrderId,
    string TransactionId) : IDomainEvent;
