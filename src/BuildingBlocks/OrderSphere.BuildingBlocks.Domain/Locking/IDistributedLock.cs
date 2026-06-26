namespace OrderSphere.BuildingBlocks.Locking;

/// <summary>
/// Acquires short-lived Redis leases so that a single instance among N replicas
/// executes a given singleton job (leader-election by lease).
/// </summary>
public interface IDistributedLock
{
    /// <summary>
    /// Attempts to acquire an exclusive lease on <paramref name="resource"/>.
    /// Returns a handle that releases the lease on disposal, or <see langword="null"/>
    /// if another instance holds the lease.
    /// </summary>
    Task<IDistributedLockHandle?> TryAcquireAsync(
        string resource,
        TimeSpan ttl,
        CancellationToken ct = default);
}
