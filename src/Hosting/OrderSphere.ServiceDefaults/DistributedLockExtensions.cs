using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Locking;
using StackExchange.Redis;

namespace Microsoft.Extensions.Hosting;

public static class DistributedLockExtensions
{
    /// <summary>
    /// Registers <see cref="IDistributedLock"/> backed by the singleton
    /// <see cref="IConnectionMultiplexer"/> already registered by
    /// <see cref="RedisExtensions.AddOrderSphereRedisAsync"/>.
    /// Must be called after that method.
    /// </summary>
    public static IServiceCollection AddOrderSphereDistributedLocking(
        this IServiceCollection services)
    {
        services.AddSingleton<IDistributedLock>(sp =>
            new RedisDistributedLock(
                sp.GetRequiredService<IConnectionMultiplexer>(),
                sp.GetRequiredService<ILogger<RedisDistributedLock>>()));
        return services;
    }
}

/// <summary>
/// Redis-backed distributed lock using SET NX PX for acquire and a Lua
/// compare-and-delete for safe release (never frees another holder's key).
/// Auto-renews at half the TTL so long-running jobs do not lose the lease.
/// </summary>
public sealed class RedisDistributedLock(
    IConnectionMultiplexer multiplexer,
    ILogger<RedisDistributedLock> logger) : IDistributedLock
{
    private static readonly Meter Meter = new("OrderSphere");

    private static readonly Counter<long> Acquired =
        Meter.CreateCounter<long>("ordersphere.lock.acquired",
            description: "Distributed lock lease acquisitions.");

    private static readonly Counter<long> Contended =
        Meter.CreateCounter<long>("ordersphere.lock.contended",
            description: "Distributed lock acquire attempts that yielded (already held).");

    // Lua: delete key only if its value matches our token. Prevents a slow holder whose
    // lease expired from releasing a lease that was re-taken by another instance.
    private const string ReleaseScript = """
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        else
            return 0
        end
        """;

    // Lua: extend TTL only if we still own the key.
    private const string RenewScript = """
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('pexpire', KEYS[1], ARGV[2])
        else
            return 0
        end
        """;

    public async Task<IDistributedLockHandle?> TryAcquireAsync(
        string resource,
        TimeSpan ttl,
        CancellationToken ct = default)
    {
        var db = multiplexer.GetDatabase();
        var key = $"lock:{resource}";
        var token = Guid.NewGuid().ToString("N");

        var acquired = await db.StringSetAsync(key, token, ttl, When.NotExists);

        if (!acquired)
        {
            Contended.Add(1, new KeyValuePair<string, object?>("resource", resource));
            logger.LogDebug("Lock '{Resource}' already held — skipping this cycle.", resource);
            return null;
        }

        Acquired.Add(1, new KeyValuePair<string, object?>("resource", resource));
        logger.LogDebug("Acquired lock '{Resource}' (ttl={Ttl:g}).", resource, ttl);

        return new RedisLockHandle(db, key, token, ttl, logger);
    }

    private sealed class RedisLockHandle : IDistributedLockHandle
    {
        private readonly IDatabase _db;
        private readonly string _key;
        private readonly string _token;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _renewCts = new();
        private readonly Task _renewTask;

        public RedisLockHandle(IDatabase db, string key, string token, TimeSpan ttl, ILogger logger)
        {
            _db = db;
            _key = key;
            _token = token;
            _logger = logger;
            _renewTask = RenewLoopAsync(ttl, _renewCts.Token);
        }

        private async Task RenewLoopAsync(TimeSpan ttl, CancellationToken ct)
        {
            var interval = ttl / 2;
            while (!ct.IsCancellationRequested)
            {
                try { await Task.Delay(interval, ct); }
                catch (OperationCanceledException) { break; }

                var extended = (long)await _db.ScriptEvaluateAsync(
                    RenewScript,
                    keys: new RedisKey[] { _key },
                    values: new RedisValue[] { _token, (long)ttl.TotalMilliseconds });

                if (extended == 0)
                {
                    _logger.LogWarning("Lock '{Key}' could not be renewed — lease expired before renewal.", _key);
                    break;
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _renewCts.CancelAsync();
            try { await _renewTask; } catch { /* renew loop already exited */ }
            _renewCts.Dispose();

            await _db.ScriptEvaluateAsync(
                ReleaseScript,
                keys: new RedisKey[] { _key },
                values: new RedisValue[] { _token });

            _logger.LogDebug("Released lock '{Key}'.", _key);
        }
    }
}
