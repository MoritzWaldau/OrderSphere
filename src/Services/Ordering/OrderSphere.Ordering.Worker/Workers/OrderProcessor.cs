using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.BuildingBlocks.ValueObjects;
using OrderSphere.Ordering.Domain.Entities;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.ValueObjects;
using OrderSphere.Ordering.Infrastructure.Persistence;

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
        using var activity = EventBusDiagnostics.StartProcess(args.Message, QueueName);
        var messageId = args.Message.MessageId;
        logger.LogInformation("Received message {MessageId}", messageId);

        try
        {
            var evt = args.Message.Body.ToObjectFromJson<CheckoutCartIntegrationEvent>();
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

    internal async Task<ProcessResult> ProcessOrderAsync(
        CheckoutCartIntegrationEvent evt,
        OrderingDbContext context,
        CancellationToken ct)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                // Idempotency check — re-evaluated on each retry attempt.
                var existingOrderId = await context.Orders
                    .Where(o => o.CorrelationId == evt.CorrelationId)
                    .Select(o => (OrderId?)o.Id)
                    .FirstOrDefaultAsync(ct);

                if (existingOrderId is not null)
                {
                    logger.LogInformation("Duplicate message for CorrelationId {CorrelationId} ignored. Existing OrderId: {OrderId}",
                        evt.CorrelationId, existingOrderId);
                    return ProcessResult.Ok();
                }

                await context.BeginTransactionAsync(ct);

                try
                {
                    var orderItems = evt.Items
                        .Select(i => new OrderItem(ProductId.From(i.ProductId), i.ProductName, Quantity.Of(i.Quantity), Money.Of(i.Price)))
                        .ToList();

                    var addr = evt.ShippingAddress;
                    var shippingAddress = new Address(addr.FirstName, addr.LastName, addr.Street, addr.City, addr.PostalCode, addr.Country);
                    var paymentMethod = Enum.Parse<PaymentMethod>(evt.PaymentMethod);

                    var order = new Order(
                        CustomerId.From(evt.CustomerId),
                        shippingAddress,
                        paymentMethod,
                        orderItems,
                        evt.CorrelationId);

                    await context.Orders.AddAsync(order, ct);

                    var paymentEvent = new PaymentRequestedIntegrationEvent
                    {
                        CorrelationId = evt.CorrelationId,
                        OrderId = order.Id.Value,
                        Amount = evt.Items.Sum(i => i.Price * i.Quantity),
                        Currency = "EUR",
                        PaymentMethod = evt.PaymentMethod,
                        CustomerEmail = evt.CustomerEmail
                    };

                    context.AddOutboxMessage(
                        nameof(PaymentRequestedIntegrationEvent),
                        JsonSerializer.Serialize(paymentEvent));

                    await context.CommitAsync(ct);

                    logger.LogInformation(
                        "Order {OrderId} created for customer {CustomerId}. CorrelationId: {CorrelationId}",
                        order.Id, order.CustomerId, evt.CorrelationId);

                    OrderingMetrics.OrdersPlaced.Add(1);

                    return ProcessResult.Ok();
                }
                catch (DbUpdateException dbEx) when (IsUniqueConstraintViolation(dbEx))
                {
                    await context.RollbackAsync(ct);
                    logger.LogWarning(dbEx, "Concurrent insert detected for CorrelationId {CorrelationId}.",
                        evt.CorrelationId);
                    return ProcessResult.Ok(); // idempotent — another instance won the race
                }
                catch
                {
                    await context.RollbackAsync(ct);
                    throw; // let the execution strategy retry on transient failures
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process order for customer {CustomerId}. CorrelationId: {CorrelationId}",
                evt.CustomerId, evt.CorrelationId);
            return ProcessResult.Fail(ex.Message);
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

    internal sealed record ProcessResult(bool IsSuccess, string? ErrorMessage)
    {
        public static ProcessResult Ok() => new(true, null);
        public static ProcessResult Fail(string error) => new(false, error);
    }
}
