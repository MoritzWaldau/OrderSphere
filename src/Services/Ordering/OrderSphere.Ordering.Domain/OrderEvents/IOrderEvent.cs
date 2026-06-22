namespace OrderSphere.Ordering.Domain.OrderEvents;

/// <summary>
/// A fact that has happened to an order, persisted in the order's event stream and folded back
/// into aggregate state on load. These are the source of truth for the write side — distinct from
/// the in-process <c>IDomainEvent</c> notifications and from the cross-service integration events.
/// </summary>
/// <remarks>
/// Payloads are intentionally primitive (Guid/string/int/decimal and simple nested records) rather
/// than value objects. The serialized form is a stored contract: keeping it free of value-object
/// internals insulates the stream from refactors to <c>Money</c>, <c>Address</c>, and the typed IDs.
/// </remarks>
public interface IOrderEvent
{
    DateTime OccurredAt { get; }
}
