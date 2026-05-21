using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderSphere.Ordering.Domain.Events;
using OrderSphere.Ordering.Infrastructure.Persistence;
using OrderSphere.Ordering.Infrastructure.ServiceBus;
using System.Text.Json;

namespace OrderSphere.Ordering.Infrastructure.Outbox;

public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    RealServiceBusPublisher publisher,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingAsync(stoppingToken);
            await Task.Delay(PollingInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessPendingAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < OutboxMessage.MaxRetries)
            .OrderBy(m => m.OccurredAt)
            .Take(20)
            .ToListAsync(ct);

        if (messages.Count == 0)
            return;

        foreach (var message in messages)
        {
            try
            {
                await DispatchAsync(message, ct);
                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;

                if (message.RetryCount >= OutboxMessage.MaxRetries)
                    logger.LogError(ex,
                        "Outbox message {Id} ({Type}) permanently failed after {MaxRetries} attempts",
                        message.Id, message.Type, OutboxMessage.MaxRetries);
                else
                    logger.LogWarning(ex,
                        "Outbox message {Id} ({Type}) failed on attempt {Attempt}/{Max}. Will retry.",
                        message.Id, message.Type, message.RetryCount, OutboxMessage.MaxRetries);
            }
        }

        await context.SaveChangesAsync(ct);
    }

    private async Task DispatchAsync(OutboxMessage message, CancellationToken ct)
    {
        if (message.Type == nameof(CheckoutCartEvent))
        {
            var evt = JsonSerializer.Deserialize<CheckoutCartEvent>(message.Content)
                ?? throw new InvalidOperationException($"Outbox message {message.Id} has null payload.");
            await publisher.PublishCheckoutCartEventAsync(evt);
            return;
        }

        throw new InvalidOperationException($"Unknown outbox message type: {message.Type}");
    }
}
