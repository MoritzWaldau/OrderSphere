using OrderSphere.BuildingBlocks.EventBus;

namespace OrderSphere.BuildingBlocks.Contracts.Events;

/// <summary>
/// Raised by Payment after a captured payment has been refunded in response to an
/// <see cref="OrderConfirmationFailedIntegrationEvent"/>. Lets Ordering advance the saga to
/// its terminal refunded state, closing the compensation loop observably.
/// </summary>
public sealed record PaymentRefundedIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public string? TransactionId { get; init; }
    public required string Reason { get; init; }
}
