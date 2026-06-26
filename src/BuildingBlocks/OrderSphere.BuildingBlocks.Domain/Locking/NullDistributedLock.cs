namespace OrderSphere.BuildingBlocks.Locking;

/// <summary>
/// No-op implementation: always grants the lease. Used as the DI fallback in hosts
/// that have not registered a Redis connection (e.g. single-instance dev, unit tests).
/// </summary>
public sealed class NullDistributedLock : IDistributedLock
{
    public static readonly NullDistributedLock Instance = new();

    public Task<IDistributedLockHandle?> TryAcquireAsync(
        string resource,
        TimeSpan ttl,
        CancellationToken ct = default)
        => Task.FromResult<IDistributedLockHandle?>(NullHandle.Instance);

    private sealed class NullHandle : IDistributedLockHandle
    {
        public static readonly NullHandle Instance = new();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
