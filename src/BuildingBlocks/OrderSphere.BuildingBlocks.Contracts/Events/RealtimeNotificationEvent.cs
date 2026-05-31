using OrderSphere.BuildingBlocks.EventBus;

namespace OrderSphere.BuildingBlocks.Contracts.Events;

public sealed record RealtimeNotificationEvent : IntegrationEvent
{
    public required string UserId { get; init; }
    public required string Type { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public Guid? OrderId { get; init; }
}
