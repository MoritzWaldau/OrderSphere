using OrderSphere.BuildingBlocks.EventBus;

namespace OrderSphere.BuildingBlocks.Contracts.Events;

/// <summary>
/// Raised by Payment after a captured payment has been refunded. Two flows produce it:
/// the saga compensation in response to an <see cref="OrderConfirmationFailedIntegrationEvent"/>
/// (advances the saga to its terminal refunded state), and the returns/RMA flow in response to a
/// <see cref="RefundRequestedIntegrationEvent"/>. When <see cref="ReturnRequestId"/> is set, the
/// refund settles a return request rather than a failed-confirmation compensation.
/// </summary>
public sealed record PaymentRefundedIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public string? TransactionId { get; init; }
    public required string Reason { get; init; }

    /// <summary>Set when the refund settles a returns/RMA request; null for saga compensation.</summary>
    public Guid? ReturnRequestId { get; init; }
}
