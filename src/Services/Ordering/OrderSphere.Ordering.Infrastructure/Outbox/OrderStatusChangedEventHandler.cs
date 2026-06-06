using System.Text.Json;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus;

namespace OrderSphere.Ordering.Infrastructure.Outbox;

internal sealed class OrderStatusChangedEventHandler(IEventBus eventBus) : IOutboxEventHandler
{
    private const string QueueName = "webhook-events";

    public string EventType => nameof(OrderStatusChangedIntegrationEvent);

    public async Task HandleAsync(string jsonPayload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<OrderStatusChangedIntegrationEvent>(jsonPayload)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize payload as {nameof(OrderStatusChangedIntegrationEvent)}.");

        await eventBus.PublishAsync(evt, QueueName, ct);
    }
}
