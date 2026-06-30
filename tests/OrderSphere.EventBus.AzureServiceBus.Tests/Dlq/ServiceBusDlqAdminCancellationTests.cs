using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Dlq;
using Xunit;

namespace OrderSphere.EventBus.AzureServiceBus.Tests.Dlq;

/// <summary>
/// Probe: does an owned-queue call observe a pre-cancelled token before attempting any Service Bus
/// I/O? If so, this exercises the clamp/receiver-setup lines deterministically and fast, with no real
/// network access. Bounded by WaitAsync so a misbehaving SDK fails the test instead of hanging it.
/// </summary>
public sealed class ServiceBusDlqAdminCancellationTests
{
    private const string FakeConnectionString =
        "Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=fake;SharedAccessKey=ZmFrZQ==";

    private static ServiceBusDlqAdmin CreateAdmin(params string[] ownedQueues) => new(
        new ServiceBusClient(FakeConnectionString),
        new DlqAdminOptions { OwnedQueues = ownedQueues },
        NullLogger<ServiceBusDlqAdmin>.Instance);

    [Fact]
    public async Task PeekAsync_ForAnOwnedQueue_WithAPreCancelledToken_ThrowsQuickly()
    {
        var admin = CreateAdmin("orders");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await admin.PeekAsync("orders", max: 10, cts.Token).WaitAsync(TimeSpan.FromSeconds(5));

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ReplayAsync_ForAnOwnedQueue_WithAPreCancelledToken_ThrowsQuickly()
    {
        var admin = CreateAdmin("orders");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await admin.ReplayAsync("orders", max: 10, cts.Token).WaitAsync(TimeSpan.FromSeconds(5));

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetDepthsAsync_ForAnOwnedQueue_WithAPreCancelledToken_ThrowsQuickly()
    {
        var admin = CreateAdmin("orders");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await admin.GetDepthsAsync(cts.Token).WaitAsync(TimeSpan.FromSeconds(5));

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
