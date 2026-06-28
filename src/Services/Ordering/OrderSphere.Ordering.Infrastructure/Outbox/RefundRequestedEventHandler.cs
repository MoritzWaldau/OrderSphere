using System.Text.Json;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus;
using OrderSphere.BuildingBlocks.EventBus.Outbox;

namespace OrderSphere.Ordering.Infrastructure.Outbox;

internal sealed class RefundRequestedEventHandler(IEventBus eventBus) : IOutboxEventHandler
{
    private const string QueueName = "refund-requested";

    public string EventType => nameof(RefundRequestedIntegrationEvent);

    public async Task HandleAsync(string jsonPayload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<RefundRequestedIntegrationEvent>(jsonPayload)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize payload as {nameof(RefundRequestedIntegrationEvent)}.");

        await eventBus.PublishAsync(evt, QueueName, ct);
    }
}
