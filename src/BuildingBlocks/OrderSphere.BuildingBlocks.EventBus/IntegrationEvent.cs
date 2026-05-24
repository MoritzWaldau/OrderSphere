namespace OrderSphere.BuildingBlocks.EventBus;

public abstract record IntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public Guid CorrelationId { get; init; }
}
