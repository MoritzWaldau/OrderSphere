using Azure.Messaging.ServiceBus;
using OrderSphere.Ordering.Domain.Events;
using System.Text.Json;

namespace OrderSphere.Ordering.Infrastructure.ServiceBus;

/// <summary>
/// Publishes events directly to Azure Service Bus.
/// Called by OutboxDispatcher — not by application handlers.
/// </summary>
public sealed class RealServiceBusPublisher(ServiceBusClient serviceBusClient)
{
    private const string QueueName = "orders";

    public async Task PublishCheckoutCartEventAsync(CheckoutCartEvent checkoutCartEvent)
    {
        var messageBody = JsonSerializer.Serialize(checkoutCartEvent);

        var message = new ServiceBusMessage(messageBody)
        {
            MessageId = Guid.NewGuid().ToString(),
        };

        await using var sender = serviceBusClient.CreateSender(QueueName);
        await sender.SendMessageAsync(message);
    }
}
