using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using OrderSphere.BuildingBlocks.Locking;
using StackExchange.Redis;
using Xunit;

namespace OrderSphere.ContainerTests;

/// <summary>
/// Verifies the Redis-backed distributed lock semantics against the
/// Aspire-provisioned Redis container. Tests run sequentially within the
/// collection so they don't interfere with each other's lock keys.
/// </summary>
[Collection(AspireAppCollection.Name)]
[Trait("Category", "Container")]
public sealed class DistributedLockContainerTests(AspireAppFixture fixture)
{
    private async Task<IDistributedLock> CreateLockAsync()
    {
        var connectionString = await fixture.ConnectionStringAsync("redis");
        var multiplexer = await ConnectionMultiplexer.ConnectAsync(connectionString);
        return new RedisDistributedLockFactory(multiplexer);
    }

    [Fact]
    public async Task TryAcquireAsync_ReturnsHandle_WhenKeyIsFree()
    {
        var distributedLock = await CreateLockAsync();

        await using var handle = await distributedLock.TryAcquireAsync(
            $"test:lock-{Guid.NewGuid():N}", TimeSpan.FromSeconds(10));

        Assert.NotNull(handle);
    }

    [Fact]
    public async Task TryAcquireAsync_ReturnsNull_WhenKeyAlreadyHeld()
    {
        var distributedLock = await CreateLockAsync();
        var resource = $"test:lock-{Guid.NewGuid():N}";

        await using var first = await distributedLock.TryAcquireAsync(resource, TimeSpan.FromSeconds(30));
        Assert.NotNull(first);

        var second = await distributedLock.TryAcquireAsync(resource, TimeSpan.FromSeconds(30));
        Assert.Null(second);
    }

    [Fact]
    public async Task DisposeAsync_ReleasesLock_AllowingReacquisition()
    {
        var distributedLock = await CreateLockAsync();
        var resource = $"test:lock-{Guid.NewGuid():N}";

        var first = await distributedLock.TryAcquireAsync(resource, TimeSpan.FromSeconds(30));
        Assert.NotNull(first);
        await first.DisposeAsync();

        await using var second = await distributedLock.TryAcquireAsync(resource, TimeSpan.FromSeconds(30));
        Assert.NotNull(second);
    }

    [Fact]
    public async Task TtlExpiry_ReleasesLock_AllowingReacquisitionByAnotherHolder()
    {
        var distributedLock = await CreateLockAsync();
        var resource = $"test:lock-{Guid.NewGuid():N}";

        // Acquire with a very short TTL and do NOT dispose — simulates a crashed instance.
        var _ = await distributedLock.TryAcquireAsync(resource, TimeSpan.FromSeconds(1));

        await Task.Delay(TimeSpan.FromSeconds(2));

        await using var reacquired = await distributedLock.TryAcquireAsync(resource, TimeSpan.FromSeconds(10));
        Assert.NotNull(reacquired);
    }

    [Fact]
    public async Task DisposeAsync_StaleHandle_DoesNotReleaseCurrentHolderLock()
    {
        var distributedLock = await CreateLockAsync();
        var resource = $"test:lock-{Guid.NewGuid():N}";

        // Acquire with a short TTL and wait for it to expire without disposing.
        var stale = await distributedLock.TryAcquireAsync(resource, TimeSpan.FromSeconds(1));
        Assert.NotNull(stale);
        await Task.Delay(TimeSpan.FromSeconds(2));

        // A second holder takes the lock.
        await using var currentHolder = await distributedLock.TryAcquireAsync(
            resource, TimeSpan.FromSeconds(30));
        Assert.NotNull(currentHolder);

        // The stale handle's dispose must NOT release the current holder's lock
        // (Lua compare-and-delete guard).
        await stale.DisposeAsync();

        // Current holder can still release it cleanly (no exception, key was not deleted).
        var thirdAttempt = await distributedLock.TryAcquireAsync(resource, TimeSpan.FromSeconds(30));
        Assert.Null(thirdAttempt);
    }
}

/// <summary>
/// Thin factory shim: tests construct a dedicated multiplexer per call to isolate
/// connections, matching production behaviour where each host owns one multiplexer.
/// </summary>
file sealed class RedisDistributedLockFactory(IConnectionMultiplexer multiplexer) : IDistributedLock
{
    private readonly RedisDistributedLock _inner =
        new(multiplexer, NullLogger<RedisDistributedLock>.Instance);

    public Task<IDistributedLockHandle?> TryAcquireAsync(
        string resource, TimeSpan ttl, CancellationToken ct = default)
        => _inner.TryAcquireAsync(resource, ttl, ct);
}
