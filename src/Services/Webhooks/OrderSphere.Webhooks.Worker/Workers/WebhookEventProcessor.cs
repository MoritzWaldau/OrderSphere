using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus.Inbox;
using OrderSphere.Webhooks.Domain.Entities;
using OrderSphere.Webhooks.Domain.Enums;
using OrderSphere.Webhooks.Infrastructure.Persistence;

namespace OrderSphere.Webhooks.Worker.Workers;

/// <summary>
/// Consumes integration events from the <c>webhook-events</c> Service Bus queue,
/// matches them against active webhook subscriptions, and creates delivery records
/// for each matching subscription.
/// </summary>
public sealed class WebhookEventProcessor(
    ServiceBusClient serviceBusClient,
    IServiceScopeFactory scopeFactory,
    ILogger<WebhookEventProcessor> logger) : BackgroundService
{
    private const string QueueName = "webhook-events";
    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = serviceBusClient.CreateProcessor(QueueName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 4,
        });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);

        // Keep the service alive until shutdown is requested.
        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        await _processor.StopProcessingAsync();
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WebhooksDbContext>();
        var inboxStore = scope.ServiceProvider.GetRequiredService<IInboxStore>();

        try
        {
            var body = args.Message.Body.ToString();
            var eventType = DetermineEventType(args.Message);

            if (eventType is null)
            {
                logger.LogWarning("Unknown message type on webhook-events queue. Completing without processing.");
                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                return;
            }

            var eventId = ExtractEventId(body);

            // Inbox check — idempotent processing.
            if (await inboxStore.HasBeenProcessedAsync(eventId, args.CancellationToken))
            {
                logger.LogInformation("Event {EventId} already processed (inbox). Skipping.", eventId);
                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                return;
            }

            // Find all active subscriptions that listen to this event type.
            var webhookEventType = MapToWebhookEventType(eventType);
            if (webhookEventType is null)
            {
                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                return;
            }

            var eventTypeName = webhookEventType.Value.ToString();
            var subscriptions = await db.Subscriptions
                .Where(s => s.IsActive && !s.IsDeleted && s.Events.Contains(eventTypeName))
                .ToListAsync(args.CancellationToken);

            // Filter precisely (Contains is a substring match; verify exact enum membership).
            var matchingSubscriptions = subscriptions
                .Where(s => s.ListensTo(webhookEventType.Value))
                .ToList();

            if (matchingSubscriptions.Count == 0)
            {
                logger.LogDebug("No active subscriptions for event type {EventType}.", eventTypeName);
                await inboxStore.MarkAsProcessedAsync(eventId, eventType, args.CancellationToken);
                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                return;
            }

            // Create a delivery record for each matching subscription.
            foreach (var sub in matchingSubscriptions)
            {
                var delivery = new WebhookDelivery(
                    sub.Id,
                    eventTypeName,
                    eventId,
                    body);

                db.Deliveries.Add(delivery);
            }

            await db.SaveChangesAsync(args.CancellationToken);
            await inboxStore.MarkAsProcessedAsync(eventId, eventType, args.CancellationToken);
            await args.CompleteMessageAsync(args.Message, args.CancellationToken);

            logger.LogInformation(
                "Created {Count} webhook deliveries for event {EventId} ({EventType}).",
                matchingSubscriptions.Count, eventId, eventTypeName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing webhook event. Message will be retried.");
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Service Bus error on {Source}.", args.ErrorSource);
        return Task.CompletedTask;
    }

    private static string? DetermineEventType(ServiceBusReceivedMessage message)
    {
        if (message.ApplicationProperties.TryGetValue("EventType", out var et) && et is string eventType)
            return eventType;

        // Try to infer from body.
        try
        {
            using var doc = JsonDocument.Parse(message.Body.ToString());
            if (doc.RootElement.TryGetProperty("Type", out var typeProp))
                return typeProp.GetString();
        }
        catch { /* Not JSON or missing property — treated as unknown. */ }

        return null;
    }

    private static Guid ExtractEventId(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("Id", out var idProp))
                return idProp.GetGuid();
        }
        catch { /* Fallback to new Guid. */ }

        return Guid.NewGuid();
    }

    private static WebhookEventType? MapToWebhookEventType(string eventType) => eventType switch
    {
        nameof(OrderPlacedIntegrationEvent) or "OrderPlaced" => WebhookEventType.OrderPlaced,
        nameof(OrderStatusChangedIntegrationEvent) or "OrderStatusChanged" => WebhookEventType.OrderStatusChanged,
        nameof(PaymentProcessedIntegrationEvent) or "PaymentCompleted" => WebhookEventType.PaymentCompleted,
        "PaymentFailed" => WebhookEventType.PaymentFailed,
        _ => null,
    };
}
