using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace OrderSphere.BuildingBlocks.EventBus.AzureServiceBus;

public sealed class AzureServiceBusEventBus(
    ServiceBusClient client,
    ILogger<AzureServiceBusEventBus> logger) : IEventBus
{
    public async Task PublishAsync<TEvent>(TEvent @event, string destination, CancellationToken ct = default)
        where TEvent : IntegrationEvent
    {
        var body = JsonSerializer.Serialize(@event, @event.GetType());

        var message = new ServiceBusMessage(body)
        {
            MessageId = @event.Id.ToString(),
            CorrelationId = @event.CorrelationId.ToString(),
            ContentType = "application/json",
            Subject = typeof(TEvent).Name,
            ApplicationProperties = { ["EventType"] = typeof(TEvent).Name }
        };

        await using var sender = client.CreateSender(destination);
        await sender.SendMessageAsync(message, ct);

        logger.LogDebug("Published {EventType} ({EventId}) to '{Destination}'.",
            typeof(TEvent).Name, @event.Id, destination);
    }
}
