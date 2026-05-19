using OrderSphere.Application.Models.Events;
using OrderSphere.Application.ServiceBus;
using OrderSphere.Infrastructure.Persistence;
using System.Text.Json;

namespace OrderSphere.Infrastructure.Outbox;

internal sealed class OutboxPublisher(OrderSphereDbContext context) : IServiceBusPublisher
{
    public Task PublishCheckoutCartEventAsync(CheckoutCartEvent checkoutCartEvent)
    {
        var message = new OutboxMessage
        {
            Type = nameof(CheckoutCartEvent),
            Content = JsonSerializer.Serialize(checkoutCartEvent),
        };

        context.OutboxMessages.Add(message);
        return Task.CompletedTask;
    }
}
