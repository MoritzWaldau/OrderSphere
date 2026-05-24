namespace OrderSphere.BuildingBlocks.EventBus.Inbox;

public sealed class InboxMessage
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = null!;
    public DateTime ProcessedAt { get; set; }
}
