using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderSphere.Ordering.Domain.Entities;
using OrderSphere.Ordering.Domain.Events;
using OrderSphere.Ordering.Domain.Services;
using OrderSphere.Ordering.Infrastructure.Persistence;
using System.Text.Json;

namespace OrderSphere.Ordering.Worker.Workers;

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
        catch (OperationCanceledException) { /* expected on shutdown */ }
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
            var evt = args.Message.Body.ToObjectFromJson<CheckoutCartEvent>();
            if (evt is null)
            {
                logger.LogError("Message {MessageId} could not be deserialized. Dead-lettering.", messageId);
                await args.DeadLetterMessageAsync(args.Message,
                    deadLetterReason: "DeserializationFailed",
                    deadLetterErrorDescription: "Body was not a valid CheckoutCartEvent.");
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

            var result = await ProcessOrderAsync(evt, context, args.CancellationToken);

            if (result.IsSuccess)
            {
                await args.CompleteMessageAsync(args.Message);
                logger.LogInformation("Message {MessageId} processed. CorrelationId: {CorrelationId}",
                    messageId, evt.CorrelationId);
            }
            else
            {
                logger.LogWarning("ProcessOrder returned failure for message {MessageId}: {Error}. Abandoning.",
                    messageId, result.ErrorMessage);
                await args.AbandonMessageAsync(args.Message);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception processing message {MessageId}. Abandoning.", messageId);
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private async Task<ProcessResult> ProcessOrderAsync(
        CheckoutCartEvent evt,
        OrderingDbContext context,
        CancellationToken ct)
    {
        var checkout = evt.CheckoutCart;

        // Idempotency check
        var existingOrderId = await context.Orders
            .Where(o => o.CorrelationId == evt.CorrelationId)
            .Select(o => (Guid?)o.Id)
            .FirstOrDefaultAsync(ct);

        if (existingOrderId is not null)
        {
            logger.LogInformation("Duplicate message for CorrelationId {CorrelationId} ignored. Existing OrderId: {OrderId}",
                evt.CorrelationId, existingOrderId);
            return ProcessResult.Ok();
        }

        try
        {
            await context.BeginTransactionAsync(ct);

            var orderItems = evt.Items
                .Select(i => new OrderItem(i.ProductId, i.ProductName, i.Quantity, i.Price))
                .ToList();

            var order = new Order(
                checkout.CustomerId,
                checkout.ShippingAddress,
                checkout.PaymentMethod,
                orderItems,
                evt.CorrelationId);

            order.Confirm(TrackingNumberGenerator.Generate());

            await context.Orders.AddAsync(order, ct);
            await context.CommitAsync(ct);

            logger.LogInformation(
                "Order {OrderId} created for customer {CustomerId}. TrackingNumber: {TrackingNumber}. CorrelationId: {CorrelationId}",
                order.Id, order.CustomerId, order.TrackingNumber, evt.CorrelationId);

            await TryPublishOrderPlacedEvent(order, evt, ct);

            return ProcessResult.Ok();
        }
        catch (DbUpdateException dbEx) when (IsUniqueConstraintViolation(dbEx))
        {
            await context.RollbackAsync(ct);
            logger.LogWarning(dbEx, "Concurrent insert detected for CorrelationId {CorrelationId}.",
                evt.CorrelationId);
            return ProcessResult.Ok(); // idempotent — another instance won the race
        }
        catch (Exception ex)
        {
            await context.RollbackAsync(ct);
            logger.LogError(ex, "Failed to process order for customer {CustomerId}. CorrelationId: {CorrelationId}",
                checkout.CustomerId, evt.CorrelationId);
            return ProcessResult.Fail(ex.Message);
        }
    }

    private async Task TryPublishOrderPlacedEvent(Order order, CheckoutCartEvent evt, CancellationToken ct)
    {
        try
        {
            var lines = evt.Items
                .Select(i => new OrderPlacedItem(i.ProductName, i.Quantity, i.Price))
                .ToList();

            var orderPlaced = new OrderPlacedEvent(
                order.Id,
                evt.CorrelationId,
                evt.CheckoutCart.CustomerEmail,
                evt.CheckoutCart.CustomerName,
                order.TrackingNumber!,
                order.ShippingAddress.FirstName,
                order.ShippingAddress.LastName,
                order.ShippingAddress.Street,
                order.ShippingAddress.City,
                order.ShippingAddress.PostalCode,
                order.ShippingAddress.Country,
                lines,
                evt.Items.Sum(i => i.Price * i.Quantity));

            await using var sender = serviceBusClient.CreateSender("notification-orders");
            var message = new ServiceBusMessage(JsonSerializer.Serialize(orderPlaced))
            {
                MessageId = order.Id.ToString(),
                ContentType = "application/json"
            };
            await sender.SendMessageAsync(message, ct);

            logger.LogInformation("OrderPlacedEvent published for order {OrderId}.", order.Id);
        }
        catch (Exception ex)
        {
            // Non-fatal: order is already committed. Notification may be delayed/missing,
            // but the order record is consistent. Log and continue.
            logger.LogWarning(ex, "Failed to publish OrderPlacedEvent for order {OrderId}.", order.Id);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        return inner is not null
            && (inner.Message.Contains("23505", StringComparison.Ordinal)
                || (inner.GetType().Name.Contains("PostgresException", StringComparison.Ordinal)
                    && inner.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)));
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

    private sealed record ProcessResult(bool IsSuccess, string? ErrorMessage)
    {
        public static ProcessResult Ok() => new(true, null);
        public static ProcessResult Fail(string error) => new(false, error);
    }
}
