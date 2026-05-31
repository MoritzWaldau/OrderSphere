using System.Collections.Concurrent;
using OrderSphere.Ordering.Application.Abstractions;

namespace OrderSphere.Ordering.Checkout.Tests;

/// <summary>
/// Process-local test double for <see cref="ICheckoutIdempotencyStore"/>.
/// TTL is irrelevant for unit tests, so entries never expire.
/// </summary>
internal sealed class InMemoryCheckoutIdempotencyStore : ICheckoutIdempotencyStore
{
    private readonly ConcurrentDictionary<string, Guid> _entries = new();

    public Task<Guid?> TryGetCorrelationIdAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(_entries.TryGetValue(key, out var id) ? id : (Guid?)null);

    public Task SetCorrelationIdAsync(string key, Guid correlationId, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        _entries[key] = correlationId;
        return Task.CompletedTask;
    }
}
