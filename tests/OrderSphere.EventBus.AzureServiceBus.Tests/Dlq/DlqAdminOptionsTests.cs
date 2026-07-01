using FluentAssertions;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Dlq;
using Xunit;

namespace OrderSphere.EventBus.AzureServiceBus.Tests.Dlq;

public sealed class DlqAdminOptionsTests
{
    [Fact]
    public void Defaults_MatchTheDocumentedCaps()
    {
        var options = new DlqAdminOptions();

        options.OwnedQueues.Should().BeEmpty();
        options.PeekCap.Should().Be(100);
        options.ReplayBatchLimit.Should().Be(50);
        options.DepthPollInterval.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void OwnedQueues_CanBeOverriddenAtInit()
    {
        var options = new DlqAdminOptions { OwnedQueues = ["orders", "payment-results"] };

        options.OwnedQueues.Should().Equal("orders", "payment-results");
    }
}

public sealed class DeadLetterMessageTests
{
    [Fact]
    public void Constructor_SetsEveryProjectedProperty()
    {
        var enqueuedTime = DateTimeOffset.UtcNow;

        var message = new DeadLetterMessage(
            MessageId: "msg-1",
            SequenceNumber: 42,
            CorrelationId: "corr-1",
            Subject: "OrderPlaced",
            DeadLetterReason: "DeserializationFailed",
            DeadLetterErrorDescription: "Body was not valid JSON.",
            EnqueuedTime: enqueuedTime,
            BodyPreview: "{ \"orderId\": 1 }");

        message.MessageId.Should().Be("msg-1");
        message.SequenceNumber.Should().Be(42);
        message.CorrelationId.Should().Be("corr-1");
        message.Subject.Should().Be("OrderPlaced");
        message.DeadLetterReason.Should().Be("DeserializationFailed");
        message.DeadLetterErrorDescription.Should().Be("Body was not valid JSON.");
        message.EnqueuedTime.Should().Be(enqueuedTime);
        message.BodyPreview.Should().Be("{ \"orderId\": 1 }");
    }
}
