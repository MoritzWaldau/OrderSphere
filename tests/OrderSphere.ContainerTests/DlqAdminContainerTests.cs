using Aspire.Hosting.ApplicationModel;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Dlq;
using OrderSphere.BuildingBlocks.Primitives;
using Xunit;

namespace OrderSphere.ContainerTests;

/// <summary>
/// Exercises the DLQ admin component against the Aspire-provisioned Service Bus emulator: a poison
/// message dead-lettered by the live Ordering worker must be visible via Peek and re-drivable via
/// Replay, and the owned-queue allow-list must reject foreign queues.
/// </summary>
[Collection(AspireAppCollection.Name)]
[Trait("Category", "Container")]
public sealed class DlqAdminContainerTests(AspireAppFixture fixture)
{
    private const string Queue = "orders";

    private async Task<ServiceBusClient> ConnectAsync()
    {
        var connectionString = await fixture.ConnectionStringAsync("azure-service-bus");
        return new ServiceBusClient(connectionString);
    }

    private static ServiceBusDlqAdmin AdminOver(ServiceBusClient client, params string[] owned) =>
        new(client, new DlqAdminOptions { OwnedQueues = owned }, NullLogger<ServiceBusDlqAdmin>.Instance);

    [Fact]
    public async Task DeadLetteredMessage_IsPeekable_AndReplayable()
    {
        // Wait for the worker that drains 'orders' so it dead-letters our poison message.
        var notify = fixture.App.Services.GetRequiredService<ResourceNotificationService>();
        await notify
            .WaitForResourceAsync("ordersphere-ordering-worker", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromMinutes(5));

        await using var client = await ConnectAsync();
        var admin = AdminOver(client, Queue);

        // Body 'null' deserialises to a null event, which OrderProcessor dead-letters immediately
        // with reason "DeserializationFailed" (no retry exhaustion needed).
        var messageId = $"dlq-test-{Guid.NewGuid():N}";
        await using (var sender = client.CreateSender(Queue))
        {
            await sender.SendMessageAsync(new ServiceBusMessage(BinaryData.FromString("null"))
            {
                MessageId = messageId,
                ContentType = "application/json"
            });
        }

        // The worker moves it to the dead-letter sub-queue within a few seconds.
        DeadLetterMessage? found = null;
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            var peek = await admin.PeekAsync(Queue, max: 100);
            Assert.True(peek.IsSuccess);

            found = peek.Value.FirstOrDefault(m => m.MessageId == messageId);
            if (found is not null)
                break;

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        Assert.NotNull(found);
        Assert.Equal("DeserializationFailed", found!.DeadLetterReason);

        // Replay re-drives dead-lettered messages back onto the main queue.
        var replay = await admin.ReplayAsync(Queue, max: 50);
        Assert.True(replay.IsSuccess);
        Assert.True(replay.Value.Replayed > 0);
    }

    [Fact]
    public async Task PeekAsync_ForUnownedQueue_FailsWithNotFound()
    {
        await using var client = await ConnectAsync();
        var admin = AdminOver(client, "orders");

        var result = await admin.PeekAsync("payment-requests", max: 10);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
