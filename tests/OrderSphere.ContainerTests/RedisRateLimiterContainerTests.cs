using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;
using Xunit;

namespace OrderSphere.ContainerTests;

/// <summary>
/// Verifies D3's core guarantee against the Aspire-provisioned Redis container: two
/// <see cref="RedisFixedWindowRateLimiter"/> instances sharing the same partition key
/// enforce one combined quota, not one quota per instance — the failure mode the
/// built-in in-process <c>FixedWindowRateLimiter</c> has when a service scales out.
/// </summary>
[Collection(AspireAppCollection.Name)]
[Trait("Category", "Container")]
public sealed class RedisRateLimiterContainerTests(AspireAppFixture fixture)
{
    private async Task<IConnectionMultiplexer> ConnectAsync()
    {
        var connectionString = await fixture.ConnectionStringAsync("redis");
        return await ConnectionMultiplexer.ConnectAsync(connectionString);
    }

    [Fact]
    public async Task TwoInstances_SharePartitionKey_EnforceCombinedQuota()
    {
        var partitionKey = $"test:ratelimit-{Guid.NewGuid():N}";

        // Two independent connections and limiter instances, simulating two service replicas
        // that happen to hit the same rate-limit partition (e.g. the same authenticated user).
        var instanceA = new RedisFixedWindowRateLimiter(
            await ConnectAsync(), partitionKey, permitLimit: 5, window: TimeSpan.FromMinutes(1));
        var instanceB = new RedisFixedWindowRateLimiter(
            await ConnectAsync(), partitionKey, permitLimit: 5, window: TimeSpan.FromMinutes(1));

        var permitted = 0;
        for (var i = 0; i < 3; i++)
        {
            using var lease = await instanceA.AcquireAsync();
            if (lease.IsAcquired) permitted++;
        }
        for (var i = 0; i < 3; i++)
        {
            using var lease = await instanceB.AcquireAsync();
            if (lease.IsAcquired) permitted++;
        }

        // Combined quota is 5 across both instances, even though each made only 3 requests
        // (which would each individually be under a per-instance limit of 5).
        Assert.Equal(5, permitted);
    }

    [Fact]
    public async Task SinglePartition_RejectsOnceLimitExceeded()
    {
        var partitionKey = $"test:ratelimit-{Guid.NewGuid():N}";
        var limiter = new RedisFixedWindowRateLimiter(
            await ConnectAsync(), partitionKey, permitLimit: 2, window: TimeSpan.FromMinutes(1));

        using var first = await limiter.AcquireAsync();
        using var second = await limiter.AcquireAsync();
        using var third = await limiter.AcquireAsync();

        Assert.True(first.IsAcquired);
        Assert.True(second.IsAcquired);
        Assert.False(third.IsAcquired);
    }

    [Fact]
    public async Task DifferentPartitionKeys_HaveIndependentQuotas()
    {
        var multiplexer = await ConnectAsync();
        var limiterA = new RedisFixedWindowRateLimiter(
            multiplexer, $"test:ratelimit-{Guid.NewGuid():N}", permitLimit: 1, window: TimeSpan.FromMinutes(1));
        var limiterB = new RedisFixedWindowRateLimiter(
            multiplexer, $"test:ratelimit-{Guid.NewGuid():N}", permitLimit: 1, window: TimeSpan.FromMinutes(1));

        using var leaseA = await limiterA.AcquireAsync();
        using var leaseB = await limiterB.AcquireAsync();

        Assert.True(leaseA.IsAcquired);
        Assert.True(leaseB.IsAcquired);
    }
}
