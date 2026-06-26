namespace OrderSphere.BuildingBlocks.Locking;

/// <summary>
/// Represents a held distributed lock lease. Disposing the handle releases the lease.
/// </summary>
public interface IDistributedLockHandle : IAsyncDisposable { }
