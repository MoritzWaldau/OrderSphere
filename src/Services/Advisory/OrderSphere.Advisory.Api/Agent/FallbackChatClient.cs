using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace OrderSphere.Advisory.Api.Agent;

// Wraps two chat-client pipelines: primary and fallback.
// On HttpRequestException, TaskCanceledException (wrapping TimeoutException), or
// TimeoutException from the primary, retries the full request against the fallback.
// Streaming: fallback is attempted only if the primary faults before the first token;
// mid-stream failures are surfaced to the caller unchanged (they already carry an
// in-progress response that cannot be restarted transparently).
public sealed class FallbackChatClient(IChatClient primary, IChatClient fallback)
    : DelegatingChatClient(primary)
{
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Probe the primary stream for its first token before yielding anything.
        // If the primary faults here we can transparently switch to the fallback.
        bool switchToFallback = false;
        bool hasFirst = false;
        ChatResponseUpdate? firstItem = default;
        IAsyncEnumerator<ChatResponseUpdate>? primaryEnum = null;

        primaryEnum = base.GetStreamingResponseAsync(messages, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        try
        {
            hasFirst = await primaryEnum.MoveNextAsync();
            if (hasFirst) firstItem = primaryEnum.Current;
        }
        catch (Exception ex) when (IsFallbackable(ex) && !cancellationToken.IsCancellationRequested)
        {
            switchToFallback = true;
            await primaryEnum.DisposeAsync();
            primaryEnum = null;
        }

        if (switchToFallback)
        {
            await foreach (var item in fallback.GetStreamingResponseAsync(messages, options, cancellationToken))
                yield return item;
            yield break;
        }

        if (!hasFirst)
        {
            await primaryEnum!.DisposeAsync();
            yield break;
        }

        yield return firstItem!;

        try
        {
            while (await primaryEnum!.MoveNextAsync())
                yield return primaryEnum.Current;
        }
        finally
        {
            await primaryEnum!.DisposeAsync();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) fallback.Dispose();
        base.Dispose(disposing);
    }

    private static bool IsFallbackable(Exception ex) => ex switch
    {
        HttpRequestException => true,
        OperationCanceledException { InnerException: TimeoutException } => true,
        TimeoutException => true,
        _ => false
    };
}
