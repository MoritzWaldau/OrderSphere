using Azure.Messaging.ServiceBus;
using MediatR;
using OrderSphere.Application.Models.Events;

namespace OrderSphere.Worker;
public sealed class OrderProcessor(
    ServiceBusClient serviceBusClient,
    IServiceScopeFactory scopeFactory,
    ILogger<OrderProcessor> logger) : BackgroundService, IAsyncDisposable
{
    private const string QueueName = "orders";
    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = serviceBusClient.CreateProcessor(QueueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 1,
            AutoCompleteMessages = false   // wir steuern Complete/Abandon selbst
        });

        _processor.ProcessMessageAsync += OnMessageReceived;
        _processor.ProcessErrorAsync += OnError;

        await _processor.StartProcessingAsync(stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);  // läuft bis Shutdown
    }

    private async Task OnMessageReceived(ProcessMessageEventArgs args)
    {
        try
        {
            var checkoutEvent = args.Message.Body.ToObjectFromJson<CheckoutCartEvent>();

            // eigener Scope weil BackgroundService Singleton ist
            await using var scope = scopeFactory.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            //await mediator.Send(new ProcessOrderCommand(checkoutEvent), args.CancellationToken);

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Verarbeiten der Order-Message {MessageId}", args.Message.MessageId);
            await args.AbandonMessageAsync(args.Message);  // → Retry bis MaxDeliveryCount
        }
    }

    private Task OnError(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Service Bus Fehler: {Source}", args.ErrorSource);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_processor is not null)
            await _processor.DisposeAsync();
    }
}