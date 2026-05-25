using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus;
using System.Text.Json;

namespace OrderSphere.Ordering.Infrastructure.Outbox;

internal sealed class PaymentRequestedEventHandler(IEventBus eventBus) : IOutboxEventHandler
{
    private const string QueueName = "payment-requests";

    public string EventType => nameof(PaymentRequestedIntegrationEvent);

    public async Task HandleAsync(string jsonPayload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<PaymentRequestedIntegrationEvent>(jsonPayload)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize payload as {nameof(PaymentRequestedIntegrationEvent)}.");

        await eventBus.PublishAsync(evt, QueueName, ct);
    }
}
