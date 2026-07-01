using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Dlq;
using OrderSphere.BuildingBlocks.Primitives;
using Xunit;

namespace OrderSphere.EventBus.AzureServiceBus.Tests.Dlq;

/// <summary>
/// Exercises the owned-queue allow-list guard, which is checked before any Service Bus call is made
/// — so these tests construct a <see cref="ServiceBusClient"/> against a fake connection string
/// (the SDK only opens a connection lazily on first I/O) and never touch the network.
/// </summary>
public sealed class ServiceBusDlqAdminTests
{
    private const string FakeConnectionString =
        "Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=fake;SharedAccessKey=ZmFrZQ==";

    private static ServiceBusDlqAdmin CreateAdmin(params string[] ownedQueues)
    {
        var client = new ServiceBusClient(FakeConnectionString);
        var options = new DlqAdminOptions { OwnedQueues = ownedQueues };
        return new ServiceBusDlqAdmin(client, options, NullLogger<ServiceBusDlqAdmin>.Instance);
    }

    [Fact]
    public void OwnedQueues_ReflectsTheConfiguredAllowList()
    {
        var admin = CreateAdmin("orders", "payment-results");

        admin.OwnedQueues.Should().Equal("orders", "payment-results");
    }

    [Fact]
    public async Task PeekAsync_ForAnUnownedQueue_FailsWithNotFoundWithoutCallingServiceBus()
    {
        var admin = CreateAdmin("orders");

        var result = await admin.PeekAsync("payment-requests", max: 10);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ReplayAsync_ForAnUnownedQueue_FailsWithNotFoundWithoutCallingServiceBus()
    {
        var admin = CreateAdmin("orders");

        var result = await admin.ReplayAsync("payment-requests", max: 10);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task PeekAsync_QueueNameComparisonIsOrdinal_DoesNotMatchDifferentCasing()
    {
        var admin = CreateAdmin("orders");

        var result = await admin.PeekAsync("Orders", max: 10);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetDepthsAsync_WithNoOwnedQueues_ReturnsAnEmptyListWithoutCallingServiceBus()
    {
        var admin = CreateAdmin();

        var result = await admin.GetDepthsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
