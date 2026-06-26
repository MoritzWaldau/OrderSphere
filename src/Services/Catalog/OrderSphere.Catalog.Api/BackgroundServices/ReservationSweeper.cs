using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Locking;
using OrderSphere.Catalog.Domain.Enums;
using OrderSphere.Catalog.Infrastructure.Persistence;

namespace OrderSphere.Catalog.Api.BackgroundServices;

/// <summary>
/// Periodically releases stock reservations whose TTL has elapsed, freeing availability
/// for abandoned checkouts. Runs in Catalog.Api so it shares the catalog database.
/// </summary>
public sealed class ReservationSweeper(
    IServiceScopeFactory scopeFactory,
    IDistributedLock distributedLock,
    ILogger<ReservationSweeper> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stock reservation sweep failed.");
            }
        }
        while (await SafeWaitAsync(timer, stoppingToken));
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        await using var handle = await distributedLock.TryAcquireAsync(
            "catalog:reservation-sweep", Interval, ct);
        if (handle is null)
            return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var now = DateTime.UtcNow;
        var expired = await context.StockReservations
            .Where(r => r.Status == ReservationStatus.Active && r.ExpiresAt <= now)
            .ToListAsync(ct);

        if (expired.Count == 0)
            return;

        foreach (var reservation in expired)
            reservation.Release();

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Released {Count} expired stock reservation(s).", expired.Count);
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try
        {
            return await timer.WaitForNextTickAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
