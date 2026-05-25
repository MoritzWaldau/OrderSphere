using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus.Inbox;
using OrderSphere.Ordering.Domain.Services;
using OrderSphere.Ordering.Infrastructure.Persistence;
using System.Text.Json;

namespace OrderSphere.Ordering.Worker.Workers;

public sealed class PaymentResultProcessor(
    ServiceBusClient serviceBusClient,
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentResultProcessor> logger) : BackgroundService
{
    private const string QueueName = "payment-results";
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
        logger.LogInformation("PaymentResultProcessor started, listening on queue '{Queue}'.", QueueName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { }
        finally
        {
            await _processor.StopProcessingAsync(CancellationToken.None);
            logger.LogInformation("PaymentResultProcessor stopped.");
        }
    }

    private async Task OnMessageReceived(ProcessMessageEventArgs args)
    {
        var messageId = args.Message.MessageId;
        logger.LogInformation("Received payment result message {MessageId}", messageId);

        try
        {
            var evt = args.Message.Body.ToObjectFromJson<PaymentProcessedIntegrationEvent>();
            if (evt is null)
            {
                logger.LogError("Message {MessageId} could not be deserialized. Dead-lettering.", messageId);
                await args.DeadLetterMessageAsync(args.Message,
                    deadLetterReason: "DeserializationFailed",
                    deadLetterErrorDescription: "Body was not a valid PaymentProcessedIntegrationEvent.");
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
            var inboxStore = scope.ServiceProvider.GetRequiredService<IInboxStore>();

            if (await inboxStore.HasBeenProcessedAsync(evt.Id))
            {
                logger.LogInformation("Event {EventId} already processed. Completing message.", evt.Id);
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            var order = await context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == evt.OrderId, args.CancellationToken);

            if (order is null)
            {
                logger.LogWarning("Order {OrderId} not found for payment result. Dead-lettering.", evt.OrderId);
                await args.DeadLetterMessageAsync(args.Message,
                    deadLetterReason: "OrderNotFound",
                    deadLetterErrorDescription: $"Order {evt.OrderId} not found.");
                return;
            }

            if (evt.Succeeded)
            {
                order.Confirm(TrackingNumberGenerator.Generate());
                logger.LogInformation("Order {OrderId} confirmed after payment. TrackingNumber: {TrackingNumber}",
                    order.Id, order.TrackingNumber);
            }
            else
            {
                order.Cancel();
                logger.LogWarning("Order {OrderId} cancelled due to payment failure: {Reason}",
                    order.Id, evt.FailureReason);
            }

            // Queue downstream events in the outbox — committed atomically with the order state
            // change and inbox record by the SaveChangesAsync inside MarkAsProcessedAsync.
            if (evt.Succeeded)
            {
                var orderPlacedEvent = new OrderPlacedIntegrationEvent
                {
                    CorrelationId = evt.CorrelationId,
                    OrderId = order.Id,
                    CustomerEmail = evt.CustomerEmail,
                    CustomerName = $"{order.ShippingAddress.FirstName} {order.ShippingAddress.LastName}",
                    TrackingNumber = order.TrackingNumber!,
                    ShippingFirstName = order.ShippingAddress.FirstName,
                    ShippingLastName = order.ShippingAddress.LastName,
                    ShippingStreet = order.ShippingAddress.Street,
                    ShippingCity = order.ShippingAddress.City,
                    ShippingPostalCode = order.ShippingAddress.PostalCode,
                    ShippingCountry = order.ShippingAddress.Country,
                    Items = order.Items
                        .Select(i => new OrderPlacedItemDto(i.ProductName, i.Quantity, i.Price))
                        .ToList(),
                    Total = order.Items.Sum(i => i.Price * i.Quantity)
                };
                context.AddOutboxMessage(
                    nameof(OrderPlacedIntegrationEvent),
                    JsonSerializer.Serialize(orderPlacedEvent));
            }

            context.AddOutboxMessage(
                nameof(RealtimeNotificationEvent),
                JsonSerializer.Serialize(new RealtimeNotificationEvent
                {
                    CorrelationId = evt.CorrelationId,
                    UserId = order.CustomerId.ToString(),
                    Type = evt.Succeeded ? "OrderConfirmed" : "OrderCancelled",
                    Title = evt.Succeeded ? "Order Confirmed" : "Order Cancelled",
                    Message = evt.Succeeded
                        ? $"Your order has been confirmed. Tracking number: {order.TrackingNumber}"
                        : $"Your order could not be processed: {evt.FailureReason ?? "Payment failed."}",
                    OrderId = order.Id
                }));

            context.AddOutboxMessage(
                nameof(OrderStatusChangedIntegrationEvent),
                JsonSerializer.Serialize(new OrderStatusChangedIntegrationEvent
                {
                    CorrelationId = evt.CorrelationId,
                    OrderId = order.Id,
                    PreviousStatus = "Pending",
                    NewStatus = evt.Succeeded ? "Confirmed" : "Cancelled",
                    CustomerEmail = evt.CustomerEmail
                }));

            // One SaveChangesAsync: order state + outbox rows + inbox mark — all atomic.
            await inboxStore.MarkAsProcessedAsync(evt.Id, nameof(PaymentProcessedIntegrationEvent));
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception processing payment result {MessageId}. Abandoning.", messageId);
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
