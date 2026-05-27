using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus;
using OrderSphere.BuildingBlocks.EventBus.Inbox;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Payment.Domain.Entities;
using OrderSphere.Payment.Infrastructure.Persistence;
using OrderSphere.Payment.Infrastructure.Providers;

namespace OrderSphere.Payment.Worker.Workers;

public sealed class PaymentProcessor(
    ServiceBusClient serviceBusClient,
    IEventBus eventBus,
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentProcessor> logger) : BackgroundService
{
    private const string QueueName = "payment-requests";
    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = serviceBusClient.CreateProcessor(QueueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 1,
            AutoCompleteMessages = false
        });

        _processor.ProcessMessageAsync += OnMessageReceived;
        _processor.ProcessErrorAsync += OnError;

        await _processor.StartProcessingAsync(stoppingToken);
        logger.LogInformation("PaymentProcessor started, listening on queue '{Queue}'.", QueueName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { }
        finally
        {
            await _processor.StopProcessingAsync(CancellationToken.None);
            logger.LogInformation("PaymentProcessor stopped.");
        }
    }

    private async Task OnMessageReceived(ProcessMessageEventArgs args)
    {
        var messageId = args.Message.MessageId;
        logger.LogInformation("Received payment request message {MessageId}", messageId);

        try
        {
            var evt = args.Message.Body.ToObjectFromJson<PaymentRequestedIntegrationEvent>();
            if (evt is null)
            {
                logger.LogError("Message {MessageId} could not be deserialized. Dead-lettering.", messageId);
                await args.DeadLetterMessageAsync(args.Message,
                    deadLetterReason: "DeserializationFailed",
                    deadLetterErrorDescription: "Body was not a valid PaymentRequestedIntegrationEvent.");
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
            var inboxStore = scope.ServiceProvider.GetRequiredService<IInboxStore>();
            var providerFactory = scope.ServiceProvider.GetRequiredService<IPaymentProviderFactory>();

            if (await inboxStore.HasBeenProcessedAsync(evt.Id))
            {
                logger.LogInformation("Event {EventId} already processed. Completing message.", evt.Id);
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            var succeeded = await ProcessPaymentAsync(evt, context, providerFactory, args.CancellationToken);

            await inboxStore.MarkAsProcessedAsync(evt.Id, nameof(PaymentRequestedIntegrationEvent));
            await context.SaveChangesAsync(args.CancellationToken);

            await PublishPaymentProcessedEvent(evt, succeeded, args.CancellationToken);

            await args.CompleteMessageAsync(args.Message);
            logger.LogInformation("Payment message {MessageId} processed. OrderId: {OrderId}, Succeeded: {Succeeded}",
                messageId, evt.OrderId, succeeded);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception processing payment message {MessageId}. Abandoning.", messageId);
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private async Task<bool> ProcessPaymentAsync(
        PaymentRequestedIntegrationEvent evt,
        PaymentDbContext context,
        IPaymentProviderFactory providerFactory,
        CancellationToken ct)
    {
        var existing = await context.Payments
            .FirstOrDefaultAsync(p => p.OrderId == OrderId.From(evt.OrderId), ct);

        if (existing is not null)
        {
            logger.LogInformation("Payment for order {OrderId} already exists with status {Status}.",
                evt.OrderId, existing.Status);
            return existing.Status is Domain.Enums.PaymentStatus.Captured or Domain.Enums.PaymentStatus.Authorized;
        }

        var record = new PaymentRecord(
            OrderId.From(evt.OrderId),
            evt.Amount,
            evt.Currency,
            evt.PaymentMethod,
            evt.CustomerEmail,
            evt.CorrelationId);

        var provider = providerFactory.GetProvider(evt.PaymentMethod);
        if (provider is null)
        {
            record.MarkFailed($"Unsupported payment method: {evt.PaymentMethod}");
            await context.Payments.AddAsync(record, ct);
            await context.SaveChangesAsync(ct);
            return false;
        }

        var request = new PaymentRequest(evt.OrderId, evt.Amount, evt.Currency, evt.CustomerEmail);
        var authResult = await provider.AuthorizeAsync(request, ct);

        if (authResult.IsFailure)
        {
            record.MarkFailed(authResult.Error.Description ?? "Authorization failed.");
            await context.Payments.AddAsync(record, ct);
            await context.SaveChangesAsync(ct);
            return false;
        }

        var captureResult = await provider.CaptureAsync(authResult.Value.TransactionId, evt.Amount, ct);

        if (captureResult.IsFailure)
        {
            record.MarkFailed(captureResult.Error.Description ?? "Capture failed.");
            await context.Payments.AddAsync(record, ct);
            await context.SaveChangesAsync(ct);
            return false;
        }

        record.MarkCaptured(captureResult.Value.TransactionId);
        await context.Payments.AddAsync(record, ct);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Payment captured for order {OrderId}. TransactionId: {TransactionId}",
            evt.OrderId, captureResult.Value.TransactionId);

        return true;
    }

    private async Task PublishPaymentProcessedEvent(
        PaymentRequestedIntegrationEvent source,
        bool succeeded,
        CancellationToken ct)
    {
        try
        {
            var processed = new PaymentProcessedIntegrationEvent
            {
                CorrelationId = source.CorrelationId,
                OrderId = source.OrderId,
                Succeeded = succeeded,
                FailureReason = succeeded ? null : "Payment processing failed.",
                CustomerEmail = source.CustomerEmail,
                PaymentMethod = source.PaymentMethod
            };

            await eventBus.PublishAsync(processed, "payment-results", ct);
            logger.LogInformation("PaymentProcessedEvent published for order {OrderId}.", source.OrderId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish PaymentProcessedEvent for order {OrderId}.", source.OrderId);
        }
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
