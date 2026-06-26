using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderSphere.Advisory.Application.Abstractions;
using OrderSphere.BuildingBlocks.Locking;

namespace OrderSphere.Advisory.Infrastructure.Cleanup;

public sealed class ConversationCleanupService(
    IServiceScopeFactory scopeFactory,
    IDistributedLock distributedLock,
    IConfiguration configuration,
    ILogger<ConversationCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan _pollingInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Conversation cleanup failed");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        await using var handle = await distributedLock.TryAcquireAsync(
            "advisory:conversation-cleanup", _pollingInterval, ct);
        if (handle is null)
            return;

        var retentionDays = configuration.GetValue("Advisor:ConversationRetentionDays", 90);
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var now = DateTime.UtcNow;

        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IAdvisoryDbContext>();

        var expiredIds = await context.Conversations
            .Where(c => c.CreatedAt < cutoff)
            .Select(c => c.Id)
            .ToListAsync(ct);

        if (expiredIds.Count == 0) return;

        await context.ConversationMessages
            .Where(m => expiredIds.Contains(m.ConversationId))
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.IsDeleted, true)
                .SetProperty(m => m.UpdatedAt, now), ct);

        await context.Conversations
            .Where(c => expiredIds.Contains(c.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsDeleted, true)
                .SetProperty(c => c.UpdatedAt, now), ct);

        logger.LogInformation(
            "Soft-deleted {Count} expired conversations (retention: {Days} days, cutoff: {Cutoff:O})",
            expiredIds.Count, retentionDays, cutoff);
    }
}
