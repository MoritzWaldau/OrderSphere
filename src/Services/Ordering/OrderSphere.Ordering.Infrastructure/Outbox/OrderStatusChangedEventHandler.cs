using System.Text.Json;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus;
using OrderSphere.BuildingBlocks.EventBus.Outbox;

namespace OrderSphere.Ordering.Infrastructure.Outbox;

internal sealed class OrderStatusChangedEventHandler(IEventBus eventBus) : IOutboxEventHandler
{
    // Fan-out targets. Service Bus uses point-to-point queues here (no topic), so each
    // consumer needs its own queue: the webhook fan-out worker and the order-history
    // read-model projector both consume this event independently.
    private const string WebhookQueue = "webhook-events";
    private const string OrderHistoryQueue = "order-history";

    public string EventType => nameof(OrderStatusChangedIntegrationEvent);

    public async Task HandleAsync(string jsonPayload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<OrderStatusChangedIntegrationEvent>(jsonPayload)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize payload as {nameof(OrderStatusChangedIntegrationEvent)}.");

        // At-least-once with idempotent consumers (inbox dedupe). If the second publish fails,
        // the outbox retries the whole handler and the first queue receives a duplicate, which
        // its consumer's inbox check discards.
        await eventBus.PublishAsync(evt, WebhookQueue, ct);
        await eventBus.PublishAsync(evt, OrderHistoryQueue, ct);
    }
}
