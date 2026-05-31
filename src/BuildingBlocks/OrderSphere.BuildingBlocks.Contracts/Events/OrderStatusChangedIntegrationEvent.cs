using OrderSphere.BuildingBlocks.EventBus;

namespace OrderSphere.BuildingBlocks.Contracts.Events;

public sealed record OrderStatusChangedIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required string PreviousStatus { get; init; }
    public required string NewStatus { get; init; }
    public required string CustomerEmail { get; init; }
}
