using System.Text.Json;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus;

namespace OrderSphere.Payment.Infrastructure.Outbox;

internal sealed class PaymentProcessedEventHandler(IEventBus eventBus) : IOutboxEventHandler
{
    private const string QueueName = "payment-results";

    public string EventType => nameof(PaymentProcessedIntegrationEvent);

    public async Task HandleAsync(string jsonPayload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<PaymentProcessedIntegrationEvent>(jsonPayload)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize payload as {nameof(PaymentProcessedIntegrationEvent)}.");

        await eventBus.PublishAsync(evt, QueueName, ct);
    }
}
