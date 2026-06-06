using Azure.Messaging.ServiceBus;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus.Inbox;
using OrderSphere.Notification.Worker.Email;

namespace OrderSphere.Notification.Worker.Workers;

public sealed class NotificationProcessor(
    ServiceBusClient serviceBusClient,
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationProcessor> logger) : BackgroundService
{
    private const string QueueName = "notification-orders";
    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = serviceBusClient.CreateProcessor(QueueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 4,
            AutoCompleteMessages = false
        });

        _processor.ProcessMessageAsync += OnMessageReceived;
        _processor.ProcessErrorAsync += OnError;

        await _processor.StartProcessingAsync(stoppingToken);
        logger.LogInformation("NotificationProcessor started, listening on queue '{Queue}'.", QueueName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { /* expected on shutdown */ }
        finally
        {
            await _processor.StopProcessingAsync(CancellationToken.None);
            logger.LogInformation("NotificationProcessor stopped.");
        }
    }

    private async Task OnMessageReceived(ProcessMessageEventArgs args)
    {
        var messageId = args.Message.MessageId;
        logger.LogInformation("Received notification message {MessageId}.", messageId);

        try
        {
            var evt = args.Message.Body.ToObjectFromJson<OrderPlacedIntegrationEvent>();
            if (evt is null)
            {
                logger.LogError("Message {MessageId} could not be deserialized. Dead-lettering.", messageId);
                await args.DeadLetterMessageAsync(args.Message,
                    deadLetterReason: "DeserializationFailed",
                    deadLetterErrorDescription: "Body was not a valid OrderPlacedIntegrationEvent.");
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var inboxStore = scope.ServiceProvider.GetRequiredService<IInboxStore>();
            var emailService = scope.ServiceProvider.GetRequiredService<INotificationEmailService>();

            await ProcessNotificationAsync(evt, inboxStore, emailService, args.CancellationToken);
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception processing notification message {MessageId}. Abandoning.", messageId);
            await args.AbandonMessageAsync(args.Message);
        }
    }

    internal async Task ProcessNotificationAsync(
        OrderPlacedIntegrationEvent evt,
        IInboxStore inboxStore,
        INotificationEmailService emailService,
        CancellationToken ct)
    {
        // Idempotency check — guard against ASB at-least-once redelivery.
        if (await inboxStore.HasBeenProcessedAsync(evt.Id, ct))
        {
            logger.LogInformation("Duplicate notification event {EventId} — skipping.", evt.Id);
            return;
        }

        if (string.IsNullOrWhiteSpace(evt.CustomerEmail))
        {
            logger.LogWarning("OrderPlacedEvent {OrderId} has no customer email. Completing without sending.", evt.OrderId);
            await inboxStore.MarkAsProcessedAsync(evt.Id, nameof(OrderPlacedIntegrationEvent), ct);
            return;
        }

        await emailService.SendOrderConfirmationAsync(evt, ct);
        await inboxStore.MarkAsProcessedAsync(evt.Id, nameof(OrderPlacedIntegrationEvent), ct);

        logger.LogInformation("Notification processed for order {OrderId}.", evt.OrderId);
    }

    private Task OnError(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception,
            "Service Bus processor error. Source: {Source}, Entity: {Entity}",
            args.ErrorSource, args.EntityPath);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.DisposeAsync();
            _processor = null;
        }
        await base.StopAsync(cancellationToken);
    }
}
