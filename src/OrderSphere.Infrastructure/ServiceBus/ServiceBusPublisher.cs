using Azure.Messaging.ServiceBus;
using OrderSphere.Application.Models.Events;
using OrderSphere.Application.ServiceBus;
using System.Text.Json;

namespace OrderSphere.Infrastructure.ServiceBus
{
    public sealed class ServiceBusPublisher(ServiceBusClient serviceBusClient) : IServiceBusPublisher
    {
        private const string QueueName = "orders";
        public async Task PublishCheckoutCartEventAsync(CheckoutCartEvent checkoutCartEvent)
        {
            var messageBody = JsonSerializer.Serialize(checkoutCartEvent);

            var message = new ServiceBusMessage(messageBody)
            {
                MessageId = Guid.NewGuid().ToString(),
            };

            await using var queueSender = serviceBusClient.CreateSender(QueueName);
            await queueSender.SendMessageAsync(message);
        }
    }
}
