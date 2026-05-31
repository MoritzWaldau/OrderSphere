namespace OrderSphere.Ordering.Application.Abstractions;

/// <summary>
/// Distributed store mapping a checkout idempotency key to the <c>CorrelationId</c>
/// produced by the first successful checkout. Duplicate requests (client retries,
/// double-submits) return the original <c>CorrelationId</c> without re-processing,
/// preventing double stock decrements.
/// <para>
/// Backed by Redis in production so the guard holds across multiple service instances —
/// a process-local cache would let a duplicate land on a second instance and re-run.
/// </para>
/// </summary>
public interface ICheckoutIdempotencyStore
{
    /// <summary>
    /// Returns the stored <c>CorrelationId</c> for <paramref name="key"/>,
    /// or <see langword="null"/> if the key has not been processed (or its TTL expired).
    /// </summary>
    Task<Guid?> TryGetCorrelationIdAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Stores <paramref name="correlationId"/> under <paramref name="key"/> with the given
    /// time-to-live. Called only after a checkout has been accepted (published to the bus).
    /// </summary>
    Task SetCorrelationIdAsync(
        string key,
        Guid correlationId,
        TimeSpan timeToLive,
        CancellationToken cancellationToken);
}
