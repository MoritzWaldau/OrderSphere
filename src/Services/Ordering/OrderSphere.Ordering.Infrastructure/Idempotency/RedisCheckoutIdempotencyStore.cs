using Microsoft.Extensions.Caching.Distributed;
using OrderSphere.Ordering.Application.Abstractions;

namespace OrderSphere.Ordering.Infrastructure.Idempotency;

/// <summary>
/// <see cref="ICheckoutIdempotencyStore"/> backed by <see cref="IDistributedCache"/>
/// (Redis in production). The <c>CorrelationId</c> is persisted as its 16-byte binary
/// representation, with expiry enforced by the cache TTL.
/// </summary>
public sealed class RedisCheckoutIdempotencyStore(IDistributedCache cache) : ICheckoutIdempotencyStore
{
    public async Task<Guid?> TryGetCorrelationIdAsync(string key, CancellationToken cancellationToken)
    {
        var bytes = await cache.GetAsync(key, cancellationToken);
        return bytes is { Length: 16 } ? new Guid(bytes) : null;
    }

    public Task SetCorrelationIdAsync(
        string key,
        Guid correlationId,
        TimeSpan timeToLive,
        CancellationToken cancellationToken) =>
        cache.SetAsync(
            key,
            correlationId.ToByteArray(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = timeToLive },
            cancellationToken);
}
