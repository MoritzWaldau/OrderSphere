using OrderSphere.Ordering.Domain.Events;
using OrderSphere.Ordering.Infrastructure.ServiceBus;
using System.Text.Json;

namespace OrderSphere.Ordering.Infrastructure.Outbox;

internal sealed class CheckoutCartEventHandler(RealServiceBusPublisher publisher) : IOutboxEventHandler
{
    public string EventType => nameof(CheckoutCartEvent);

    public async Task HandleAsync(string jsonPayload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<CheckoutCartEvent>(jsonPayload)
            ?? throw new InvalidOperationException($"Failed to deserialize payload as {nameof(CheckoutCartEvent)}.");

        await publisher.PublishCheckoutCartEventAsync(evt);
    }
}
