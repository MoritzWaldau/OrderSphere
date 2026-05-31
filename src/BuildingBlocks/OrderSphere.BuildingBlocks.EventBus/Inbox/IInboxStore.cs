namespace OrderSphere.BuildingBlocks.EventBus.Inbox;

public interface IInboxStore
{
    Task<bool> HasBeenProcessedAsync(Guid eventId, CancellationToken ct = default);
    Task MarkAsProcessedAsync(Guid eventId, string eventType, CancellationToken ct = default);
}
