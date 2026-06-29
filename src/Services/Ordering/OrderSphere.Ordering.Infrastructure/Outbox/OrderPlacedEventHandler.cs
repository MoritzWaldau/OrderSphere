using System.Text.Json;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus;
using OrderSphere.BuildingBlocks.EventBus.Outbox;

namespace OrderSphere.Ordering.Infrastructure.Outbox;

internal sealed class OrderPlacedEventHandler(IEventBus eventBus) : IOutboxEventHandler
{
    // Fan-out targets. Service Bus uses point-to-point queues here (no topic), so each
    // consumer needs its own queue: the notification worker sends the confirmation email and
    // the invoicing service generates the invoice PDF, both off the same OrderPlaced event.
    private const string NotificationQueue = "notification-orders";
    private const string InvoiceQueue = "invoice-generation";

    public string EventType => nameof(OrderPlacedIntegrationEvent);

    public async Task HandleAsync(string jsonPayload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<OrderPlacedIntegrationEvent>(jsonPayload)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize payload as {nameof(OrderPlacedIntegrationEvent)}.");

        // At-least-once with idempotent consumers (inbox dedupe). If the second publish fails,
        // the outbox retries the whole handler and the first queue receives a duplicate, which
        // its consumer's inbox check discards.
        await eventBus.PublishAsync(evt, NotificationQueue, ct);
        await eventBus.PublishAsync(evt, InvoiceQueue, ct);
    }
}
