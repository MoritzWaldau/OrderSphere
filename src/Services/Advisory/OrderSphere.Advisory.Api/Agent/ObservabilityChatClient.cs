using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using OrderSphere.BuildingBlocks.Diagnostics;

namespace OrderSphere.Advisory.Api.Agent;

/// <summary>
/// DelegatingChatClient decorator that records cost and quality metrics for each
/// model-backed advisory turn on the shared "OrderSphere" meter:
/// <list type="bullet">
///   <item>token consumption (input/output), summed across the turn's model round-trips;</item>
///   <item>tool invocations, tagged by tool name and outcome (success|error);</item>
///   <item>wall-clock turn latency, tagged by outcome (completed|canceled|failed).</item>
/// </list>
/// A single call to <see cref="GetStreamingResponseAsync"/> corresponds to one full
/// turn: the inner <c>FunctionInvokingChatClient</c> runs its entire model→tool→model
/// loop within that one streamed response, so every <see cref="FunctionCallContent"/>,
/// <see cref="FunctionResultContent"/> and <see cref="UsageContent"/> for the turn
/// passes through here and can be aggregated.
///
/// The decorator is registered inside the semantic-cache decorator, so cache hits —
/// which consume no tokens and call no tools — are excluded; cache effectiveness is
/// tracked separately by the cache hit/miss counters. These business metrics
/// complement the per-round-trip GenAI spans emitted by UseOpenTelemetry.
/// </summary>
public sealed class ObservabilityChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        long inputTokens = 0;
        long outputTokens = 0;
        // CallId → tool name, so a streamed function result can be attributed to its tool.
        var toolNames = new Dictionary<string, string>();
        var completed = false;

        // yield is illegal inside a try with a catch clause, so the inner stream is
        // consumed via its enumerator; the finally records the turn whether it finishes,
        // is canceled, or throws (the exception then propagates to the caller).
        var enumerator = base.GetStreamingResponseAsync(messages, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        try
        {
            while (await enumerator.MoveNextAsync())
            {
                var update = enumerator.Current;

                foreach (var content in update.Contents)
                {
                    switch (content)
                    {
                        case FunctionCallContent call:
                            toolNames[call.CallId] = call.Name;
                            break;
                        case FunctionResultContent result:
                            RecordToolInvocation(toolNames, result);
                            break;
                        case UsageContent usage:
                            inputTokens += usage.Details.InputTokenCount ?? 0;
                            outputTokens += usage.Details.OutputTokenCount ?? 0;
                            break;
                    }
                }

                yield return update;
            }

            completed = true;
        }
        finally
        {
            await enumerator.DisposeAsync();
            stopwatch.Stop();

            var outcome = completed
                ? "completed"
                : cancellationToken.IsCancellationRequested ? "canceled" : "failed";

            RecordTurn(stopwatch.Elapsed.TotalMilliseconds, inputTokens, outputTokens, outcome);
        }
    }

    private static void RecordToolInvocation(
        IReadOnlyDictionary<string, string> toolNames, FunctionResultContent result)
    {
        var toolName = toolNames.TryGetValue(result.CallId, out var name) ? name : "unknown";

        // On the live stream (not a deserialized session) Exception is reliably populated
        // on failure; the JsonIgnore caveat applies only after deserialization.
        var outcome = result.Exception is null ? "success" : "error";

        ApplicationDiagnostics.AdvisorToolInvocations.Add(
            1,
            new KeyValuePair<string, object?>("tool", toolName),
            new KeyValuePair<string, object?>("outcome", outcome));
    }

    private static void RecordTurn(double elapsedMs, long inputTokens, long outputTokens, string outcome)
    {
        ApplicationDiagnostics.AdvisorTurnDuration.Record(
            elapsedMs, new KeyValuePair<string, object?>("outcome", outcome));

        if (inputTokens > 0)
        {
            ApplicationDiagnostics.AdvisorTokens.Add(
                inputTokens, new KeyValuePair<string, object?>("direction", "input"));
        }

        if (outputTokens > 0)
        {
            ApplicationDiagnostics.AdvisorTokens.Add(
                outputTokens, new KeyValuePair<string, object?>("direction", "output"));
        }
    }
}
