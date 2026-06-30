using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Dlq;

/// <summary>
/// <see cref="IDlqAdmin"/> backed by a <see cref="ServiceBusClient"/>. Dead-letter access uses a
/// receiver on the <see cref="SubQueue.DeadLetter"/> sub-queue; replay receives in peek-lock mode,
/// re-sends a clone to the main queue, and completes the dead-letter copy.
/// </summary>
public sealed class ServiceBusDlqAdmin(
    ServiceBusClient client,
    DlqAdminOptions options,
    ILogger<ServiceBusDlqAdmin> logger) : IDlqAdmin
{
    private const int BodyPreviewLength = 512;

    public IReadOnlyList<string> OwnedQueues => options.OwnedQueues;

    public async Task<Result<IReadOnlyList<DlqQueueDepth>>> GetDepthsAsync(CancellationToken ct = default)
    {
        var depths = new List<DlqQueueDepth>(options.OwnedQueues.Count);
        foreach (var queue in options.OwnedQueues)
        {
            var count = await CountDeadLettersAsync(queue, ct);
            depths.Add(new DlqQueueDepth(queue, count, Capped: count >= options.PeekCap));
        }

        return Result<IReadOnlyList<DlqQueueDepth>>.Success(depths);
    }

    public async Task<Result<IReadOnlyList<DeadLetterMessage>>> PeekAsync(string queue, int max, CancellationToken ct = default)
    {
        if (!IsOwned(queue))
            return Result<IReadOnlyList<DeadLetterMessage>>.Failure(UnknownQueue(queue));

        var take = Math.Clamp(max, 1, options.PeekCap);

        await using var receiver = client.CreateReceiver(queue, new ServiceBusReceiverOptions
        {
            SubQueue = SubQueue.DeadLetter
        });

        var messages = await receiver.PeekMessagesAsync(take, fromSequenceNumber: null, ct);
        var projected = messages.Select(Project).ToList();

        return Result<IReadOnlyList<DeadLetterMessage>>.Success(projected);
    }

    public async Task<Result<DlqReplayReport>> ReplayAsync(string queue, int max, CancellationToken ct = default)
    {
        if (!IsOwned(queue))
            return Result<DlqReplayReport>.Failure(UnknownQueue(queue));

        var take = Math.Clamp(max, 1, options.ReplayBatchLimit);

        await using var receiver = client.CreateReceiver(queue, new ServiceBusReceiverOptions
        {
            SubQueue = SubQueue.DeadLetter,
            ReceiveMode = ServiceBusReceiveMode.PeekLock
        });
        await using var sender = client.CreateSender(queue);

        var received = await receiver.ReceiveMessagesAsync(take, maxWaitTime: TimeSpan.FromSeconds(5), ct);
        var replayed = 0;

        foreach (var message in received)
        {
            // Re-send first, then complete: if completing fails the message is redelivered to the DLQ
            // and the resend duplicates — downstream inbox dedup (event id) makes the retry idempotent.
            await sender.SendMessageAsync(CloneForResubmit(message), ct);
            await receiver.CompleteMessageAsync(message, ct);
            replayed++;
        }

        if (replayed > 0)
            logger.LogInformation("Replayed {Count} dead-lettered message(s) to queue '{Queue}'.", replayed, queue);

        return Result<DlqReplayReport>.Success(new DlqReplayReport(queue, replayed));
    }

    private async Task<int> CountDeadLettersAsync(string queue, CancellationToken ct)
    {
        await using var receiver = client.CreateReceiver(queue, new ServiceBusReceiverOptions
        {
            SubQueue = SubQueue.DeadLetter
        });

        // Peek is the emulator-safe depth source. It is bounded by PeekCap, so the gauge reports an
        // approximate "at least N" for very deep queues; exact counts come from the platform
        // DeadLetteredMessageCount metric in production (see docs/operations.md).
        var messages = await receiver.PeekMessagesAsync(options.PeekCap, fromSequenceNumber: null, ct);
        return messages.Count;
    }

    private bool IsOwned(string queue) =>
        options.OwnedQueues.Contains(queue, StringComparer.Ordinal);

    private static Error UnknownQueue(string queue) =>
        new("Dlq.UnknownQueue", $"Queue '{queue}' is not owned by this host.", ErrorType.NotFound);

    private static DeadLetterMessage Project(ServiceBusReceivedMessage m) => new(
        MessageId: m.MessageId,
        SequenceNumber: m.SequenceNumber,
        CorrelationId: m.CorrelationId,
        Subject: m.Subject,
        DeadLetterReason: m.DeadLetterReason,
        DeadLetterErrorDescription: m.DeadLetterErrorDescription,
        EnqueuedTime: m.EnqueuedTime,
        BodyPreview: Truncate(m.Body.ToString()));

    private static ServiceBusMessage CloneForResubmit(ServiceBusReceivedMessage m)
    {
        var clone = new ServiceBusMessage(m.Body)
        {
            MessageId = m.MessageId,
            CorrelationId = m.CorrelationId,
            ContentType = m.ContentType,
            Subject = m.Subject
        };

        foreach (var property in m.ApplicationProperties)
            clone.ApplicationProperties[property.Key] = property.Value;

        return clone;
    }

    private static string Truncate(string body) =>
        body.Length <= BodyPreviewLength ? body : body[..BodyPreviewLength];
}
