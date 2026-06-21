using System.Text.Json;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus;
using OrderSphere.BuildingBlocks.EventBus.Outbox;

namespace OrderSphere.Ordering.Infrastructure.Outbox;

internal sealed class OrderConfirmationFailedEventHandler(IEventBus eventBus) : IOutboxEventHandler
{
    private const string QueueName = "order-confirmation-failed";

    public string EventType => nameof(OrderConfirmationFailedIntegrationEvent);

    public async Task HandleAsync(string jsonPayload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<OrderConfirmationFailedIntegrationEvent>(jsonPayload)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize payload as {nameof(OrderConfirmationFailedIntegrationEvent)}.");

        await eventBus.PublishAsync(evt, QueueName, ct);
    }
}
