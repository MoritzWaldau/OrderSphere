using OrderSphere.BuildingBlocks.EventBus;

namespace OrderSphere.BuildingBlocks.Contracts.Events;

public sealed record PaymentRequestedIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string PaymentMethod { get; init; }
    public required string CustomerEmail { get; init; }
}
