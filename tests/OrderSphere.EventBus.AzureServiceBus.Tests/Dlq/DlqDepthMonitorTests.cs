using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Dlq;
using OrderSphere.BuildingBlocks.Primitives;
using Xunit;

namespace OrderSphere.EventBus.AzureServiceBus.Tests.Dlq;

public sealed class DlqDepthMonitorTests
{
    private sealed class FakeDlqAdmin(Func<Result<IReadOnlyList<DlqQueueDepth>>> getDepths) : IDlqAdmin
    {
        public IReadOnlyList<string> OwnedQueues => [];

        public Task<Result<IReadOnlyList<DlqQueueDepth>>> GetDepthsAsync(CancellationToken ct = default) =>
            Task.FromResult(getDepths());

        public Task<Result<IReadOnlyList<DeadLetterMessage>>> PeekAsync(string queue, int max, CancellationToken ct = default) =>
            throw new NotSupportedException("Not used by the depth monitor.");

        public Task<Result<DlqReplayReport>> ReplayAsync(string queue, int max, CancellationToken ct = default) =>
            throw new NotSupportedException("Not used by the depth monitor.");
    }

    private static DlqAdminOptions FastPollOptions() => new()
    {
        DepthPollInterval = TimeSpan.FromMilliseconds(20)
    };

    [Fact]
    public async Task ExecuteAsync_OnSuccess_WritesEveryQueueDepthIntoTheCache()
    {
        var admin = new FakeDlqAdmin(() => Result<IReadOnlyList<DlqQueueDepth>>.Success(
        [
            new DlqQueueDepth("orders", 2, Capped: false),
            new DlqQueueDepth("payment-results", 0, Capped: false)
        ]));
        var cache = new DlqDepthCache();
        var monitor = new DlqDepthMonitor(admin, cache, FastPollOptions(), NullLogger<DlqDepthMonitor>.Instance);

        await monitor.StartAsync(CancellationToken.None);
        await WaitUntil(() => cache.Snapshot().Count == 2);
        await monitor.StopAsync(CancellationToken.None);

        cache.Snapshot()["orders"].Should().Be(2);
        cache.Snapshot()["payment-results"].Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WhenThePollThrows_SwallowsTheExceptionAndKeepsRunning()
    {
        var pollCount = 0;
        var admin = new FakeDlqAdmin(() =>
        {
            pollCount++;
            throw new InvalidOperationException("transient Service Bus error");
        });
        var cache = new DlqDepthCache();
        var monitor = new DlqDepthMonitor(admin, cache, FastPollOptions(), NullLogger<DlqDepthMonitor>.Instance);

        await monitor.StartAsync(CancellationToken.None);
        await WaitUntil(() => pollCount >= 2);

        var stop = async () => await monitor.StopAsync(CancellationToken.None);
        await stop.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheAdminReturnsFailure_DoesNotWriteToTheCache()
    {
        var admin = new FakeDlqAdmin(() => Result<IReadOnlyList<DlqQueueDepth>>.Failure(
            new Error("Dlq.Unavailable", "boom", ErrorType.Failure)));
        var cache = new DlqDepthCache();
        var monitor = new DlqDepthMonitor(admin, cache, FastPollOptions(), NullLogger<DlqDepthMonitor>.Instance);

        await monitor.StartAsync(CancellationToken.None);
        await Task.Delay(60);
        await monitor.StopAsync(CancellationToken.None);

        cache.Snapshot().Should().BeEmpty();
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(10);

        condition().Should().BeTrue("the background poll should have run within the timeout");
    }

    private sealed class CancelAwareDlqAdmin : IDlqAdmin
    {
        public IReadOnlyList<string> OwnedQueues => [];

        public async Task<Result<IReadOnlyList<DlqQueueDepth>>> GetDepthsAsync(CancellationToken ct = default)
        {
            // Blocks until the host cancels the stopping token, mirroring an in-flight Service Bus
            // call that's still pending when the worker shuts down.
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException("unreachable: Task.Delay(Infinite) only returns by cancelling.");
        }

        public Task<Result<IReadOnlyList<DeadLetterMessage>>> PeekAsync(string queue, int max, CancellationToken ct = default) =>
            throw new NotSupportedException("Not used by the depth monitor.");

        public Task<Result<DlqReplayReport>> ReplayAsync(string queue, int max, CancellationToken ct = default) =>
            throw new NotSupportedException("Not used by the depth monitor.");
    }

    [Fact]
    public async Task ExecuteAsync_WhenStoppedWhileAwaitingTheAdmin_BreaksTheLoopCleanly()
    {
        var admin = new CancelAwareDlqAdmin();
        var cache = new DlqDepthCache();
        var monitor = new DlqDepthMonitor(admin, cache, FastPollOptions(), NullLogger<DlqDepthMonitor>.Instance);

        await monitor.StartAsync(CancellationToken.None);
        await Task.Delay(20);

        var stop = async () => await monitor.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        await stop.Should().NotThrowAsync();
    }
}
