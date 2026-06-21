using OrderSphere.BuildingBlocks.EventBus;

namespace OrderSphere.BuildingBlocks.Contracts.Events;

/// <summary>
/// Raised by Ordering when a payment succeeded (money captured) but the order/reservation
/// confirmation could not complete after bounded retries. Signals Payment to actively refund
/// the captured payment, making the saga's failure completion deterministic rather than
/// relying on the reservation TTL sweeper.
/// </summary>
public sealed record OrderConfirmationFailedIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string Reason { get; init; }
    public required string CustomerEmail { get; init; }
    public required string PaymentMethod { get; init; }
}
