using OrderSphere.BuildingBlocks.EventBus;

namespace OrderSphere.BuildingBlocks.Contracts.Events;

/// <summary>
/// Raised by Ordering when a staff member approves a return request (RMA). Signals Payment to
/// refund the captured payment for the order. The refund result flows back as
/// <see cref="PaymentRefundedIntegrationEvent"/> carrying the same <see cref="ReturnRequestId"/>,
/// which lets Ordering advance the originating return request to its terminal refunded state.
/// </summary>
public sealed record RefundRequestedIntegrationEvent : IntegrationEvent
{
    public required Guid ReturnRequestId { get; init; }
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string Reason { get; init; }
}
