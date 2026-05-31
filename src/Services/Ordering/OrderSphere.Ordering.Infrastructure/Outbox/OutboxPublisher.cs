using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Domain.Events;
using OrderSphere.Ordering.Infrastructure.Persistence;
using System.Text.Json;

namespace OrderSphere.Ordering.Infrastructure.Outbox;

/// <summary>
/// Writes CheckoutCartEvent to the ordering outbox table instead of publishing directly.
/// OutboxDispatcher picks it up and dispatches to Service Bus.
/// </summary>
internal sealed class OutboxPublisher(OrderingDbContext context) : IOrderingServiceBusPublisher
{
    public async Task PublishCheckoutCartEventAsync(CheckoutCartEvent checkoutCartEvent, CancellationToken ct = default)
    {
        var message = new OutboxMessage
        {
            Type = nameof(CheckoutCartEvent),
            Content = JsonSerializer.Serialize(checkoutCartEvent),
        };

        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync(ct);
    }
}
