using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Models.Events;
using OrderSphere.Infrastructure.Persistence;
using OrderSphere.Infrastructure.ServiceBus;
using System.Text.Json;

namespace OrderSphere.Infrastructure.Outbox;

public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    ServiceBusPublisher publisher,
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
        var context = scope.ServiceProvider.GetRequiredService<OrderSphereDbContext>();

        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.Error == null)
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
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to dispatch outbox message {Id} of type {Type}", message.Id, message.Type);
                message.Error = ex.Message;
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
