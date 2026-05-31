using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderSphere.Ordering.Infrastructure.Persistence;

namespace OrderSphere.Ordering.Infrastructure.Outbox;

/// <summary>
/// Deletes processed outbox messages older than 7 days. Runs once daily.
/// </summary>
public sealed class OutboxCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromDays(1);
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(7);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CleanupInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CleanupAsync(stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

            var cutoff = DateTime.UtcNow - RetentionPeriod;
            var deleted = await context.OutboxMessages
                .Where(m => m.ProcessedAt != null && m.ProcessedAt < cutoff)
                .ExecuteDeleteAsync(ct);

            if (deleted > 0)
                logger.LogInformation(
                    "OutboxCleanup removed {Count} processed outbox messages older than {Days} days.",
                    deleted, (int)RetentionPeriod.TotalDays);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OutboxCleanup failed.");
        }
    }
}
