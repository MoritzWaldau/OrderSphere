using System.Text.Json;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus;
using OrderSphere.BuildingBlocks.EventBus.Outbox;

namespace OrderSphere.UserProfile.Infrastructure.Outbox;

/// <summary>
/// Fan-out targets for D1 (GDPR right-to-erasure). Service Bus uses point-to-point queues here
/// (no topic), so each PII-holding consumer needs its own queue.
/// </summary>
internal sealed class CustomerErasureRequestedEventHandler(IEventBus eventBus) : IOutboxEventHandler
{
    private const string OrderingQueue = "erasure-ordering";
    private const string PaymentQueue = "erasure-payment";
    private const string InvoicingQueue = "erasure-invoicing";
    private const string AdvisoryQueue = "erasure-advisory";

    public string EventType => nameof(CustomerErasureRequestedIntegrationEvent);

    public async Task HandleAsync(string jsonPayload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<CustomerErasureRequestedIntegrationEvent>(jsonPayload)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize payload as {nameof(CustomerErasureRequestedIntegrationEvent)}.");

        // At-least-once with idempotent consumers (inbox dedupe). If a later publish fails, the
        // outbox retries the whole handler and earlier queues receive a duplicate, which each
        // consumer's inbox check discards.
        await eventBus.PublishAsync(evt, OrderingQueue, ct);
        await eventBus.PublishAsync(evt, PaymentQueue, ct);
        await eventBus.PublishAsync(evt, InvoicingQueue, ct);
        await eventBus.PublishAsync(evt, AdvisoryQueue, ct);
    }
}
