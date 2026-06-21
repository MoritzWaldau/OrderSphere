using System.Text.Json;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus;
using OrderSphere.BuildingBlocks.EventBus.Outbox;

namespace OrderSphere.Payment.Infrastructure.Outbox;

internal sealed class PaymentRefundedEventHandler(IEventBus eventBus) : IOutboxEventHandler
{
    private const string QueueName = "payment-refunds";

    public string EventType => nameof(PaymentRefundedIntegrationEvent);

    public async Task HandleAsync(string jsonPayload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<PaymentRefundedIntegrationEvent>(jsonPayload)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize payload as {nameof(PaymentRefundedIntegrationEvent)}.");

        await eventBus.PublishAsync(evt, QueueName, ct);
    }
}
