using System.Diagnostics.Metrics;
using System.Threading.RateLimiting;
using StackExchange.Redis;

namespace Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Builds <see cref="RateLimitPartition{TKey}"/> instances backed by <see cref="RedisFixedWindowRateLimiter"/>
/// instead of the in-process counters .NET's built-in <c>RateLimitPartition.GetFixedWindowLimiter</c> uses.
/// Every OrderSphere instance sharing the same Redis key therefore enforces one combined quota rather than
/// one quota per instance.
/// </summary>
public static class RedisRateLimitPartition
{
    public static RateLimitPartition<string> GetRedisFixedWindowLimiter(
        string partitionKey,
        IConnectionMultiplexer multiplexer,
        int permitLimit,
        TimeSpan window) =>
        RateLimitPartition.Get(partitionKey,
            _ => new RedisFixedWindowRateLimiter(multiplexer, partitionKey, permitLimit, window));
}

/// <summary>
/// Fixed-window rate limiter using an atomic Redis <c>INCR</c> + <c>PEXPIRE</c> Lua script per window,
/// analogous in style to <c>Microsoft.Extensions.Hosting.RedisDistributedLock</c>. Stateless beyond the
/// Redis key — safe to share the partition key across process instances to enforce one combined quota.
/// </summary>
public sealed class RedisFixedWindowRateLimiter(
    IConnectionMultiplexer multiplexer,
    string partitionKey,
    int permitLimit,
    TimeSpan window) : RateLimiter
{
    private static readonly Meter Meter = new("OrderSphere");

    private static readonly Counter<long> Permitted =
        Meter.CreateCounter<long>("ordersphere.ratelimit.permitted",
            description: "Rate-limited requests allowed through.");

    private static readonly Counter<long> Rejected =
        Meter.CreateCounter<long>("ordersphere.ratelimit.rejected",
            description: "Rate-limited requests rejected (quota exceeded).");

    // Lua: increment the window counter and, only on the first hit of a fresh window,
    // set its expiry — matching the fixed-window semantics of AddFixedWindowLimiter.
    private const string IncrementScript = """
        local count = redis.call('INCRBY', KEYS[1], ARGV[1])
        if count == tonumber(ARGV[1]) then
            redis.call('PEXPIRE', KEYS[1], ARGV[2])
        end
        return count
        """;

    private static readonly RateLimitLease PermittedLease = new BooleanLease(true);
    private static readonly RateLimitLease RejectedLease = new BooleanLease(false);

    public override TimeSpan? IdleDuration => null;

    public override RateLimiterStatistics? GetStatistics() => null;

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        var db = multiplexer.GetDatabase();
        var count = (long)db.ScriptEvaluate(
            IncrementScript,
            keys: [RedisKey()],
            values: [permitCount, (long)window.TotalMilliseconds]);

        return Lease(count);
    }

    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(
        int permitCount, CancellationToken cancellationToken)
    {
        var db = multiplexer.GetDatabase();
        var count = (long)await db.ScriptEvaluateAsync(
            IncrementScript,
            keys: [RedisKey()],
            values: [permitCount, (long)window.TotalMilliseconds]).WaitAsync(cancellationToken);

        return Lease(count);
    }

    private RateLimitLease Lease(long countAfterIncrement)
    {
        if (countAfterIncrement <= permitLimit)
        {
            Permitted.Add(1, new KeyValuePair<string, object?>("partition", partitionKey));
            return PermittedLease;
        }

        Rejected.Add(1, new KeyValuePair<string, object?>("partition", partitionKey));
        return RejectedLease;
    }

    private RedisKey RedisKey() => $"ratelimit:{partitionKey}";

    private sealed class BooleanLease(bool isAcquired) : RateLimitLease
    {
        public override bool IsAcquired => isAcquired;
        public override IEnumerable<string> MetadataNames => [];
        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = null;
            return false;
        }
    }
}
