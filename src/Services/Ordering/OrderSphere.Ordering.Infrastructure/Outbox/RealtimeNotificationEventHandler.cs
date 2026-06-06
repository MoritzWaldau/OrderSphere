using System.Text.Json;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus;

namespace OrderSphere.Ordering.Infrastructure.Outbox;

internal sealed class RealtimeNotificationEventHandler(IEventBus eventBus) : IOutboxEventHandler
{
    private const string QueueName = "realtime-notifications";

    public string EventType => nameof(RealtimeNotificationEvent);

    public async Task HandleAsync(string jsonPayload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<RealtimeNotificationEvent>(jsonPayload)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize payload as {nameof(RealtimeNotificationEvent)}.");

        await eventBus.PublishAsync(evt, QueueName, ct);
    }
}
