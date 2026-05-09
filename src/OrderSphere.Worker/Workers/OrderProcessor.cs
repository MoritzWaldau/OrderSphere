using Azure.Messaging.ServiceBus;
using MediatR;
using OrderSphere.Application.Features.Order.ProcessOrder;
using OrderSphere.Application.Models.Events;

namespace OrderSphere.Worker.Workers;

public sealed class OrderProcessor(
    ServiceBusClient serviceBusClient,
    IServiceScopeFactory scopeFactory,
    ILogger<OrderProcessor> logger) : BackgroundService
{
    private const string QueueName = "orders";
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

        logger.LogInformation("OrderProcessor started, listening on queue '{Queue}'.", QueueName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
        finally
        {
            await _processor.StopProcessingAsync(CancellationToken.None);
            logger.LogInformation("OrderProcessor stopped.");
        }
    }

    private async Task OnMessageReceived(ProcessMessageEventArgs args)
    {
        var messageId = args.Message.MessageId;
        logger.LogInformation("Received message {MessageId}", messageId);

        try
        {
            var checkoutEvent = args.Message.Body.ToObjectFromJson<CheckoutCartEvent>();
            if (checkoutEvent is null)
            {
                logger.LogError("Message {MessageId} could not be deserialized to CheckoutCartEvent. Dead-lettering.",
                    messageId);
                await args.DeadLetterMessageAsync(args.Message,
                    deadLetterReason: "DeserializationFailed",
                    deadLetterErrorDescription: "Body was not a valid CheckoutCartEvent.");
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.Send(
                new ProcessOrderCommand(checkoutEvent),
                args.CancellationToken);

            if (result.IsSuccess)
            {
                await args.CompleteMessageAsync(args.Message);
                logger.LogInformation(
                    "Message {MessageId} processed successfully. CorrelationId: {CorrelationId}",
                    messageId, checkoutEvent.CorrelationId);
            }
            else
            {
                logger.LogWarning(
                    "ProcessOrderCommand returned failure for message {MessageId}: {Error}. Abandoning.",
                    messageId, result.Error?.Code);
                await args.AbandonMessageAsync(args.Message);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unhandled exception processing message {MessageId}. Abandoning for retry.",
                messageId);
            await args.AbandonMessageAsync(args.Message);
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
