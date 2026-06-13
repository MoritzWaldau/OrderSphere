namespace OrderSphere.BuildingBlocks.EventBus.Outbox;

public interface IOutboxEventHandler
{
    string EventType { get; }
    Task HandleAsync(string jsonPayload, CancellationToken ct);
}
