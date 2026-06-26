using Azure.AI.ContentSafety;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;

namespace OrderSphere.Advisory.Api.Agent;

public interface IAdvisorChatClientFactory
{
    /// <summary>
    /// The shared chat-client pipeline, or <c>null</c> when Foundry is not configured.
    /// Callers degrade gracefully instead of failing.
    /// </summary>
    IChatClient? GetChatClient();
}

// Singleton. DefaultAzureCredential and AzureOpenAIClient are expensive to construct
// and thread-safe, so the whole pipeline is built once and shared across requests.
// Per-user state (MCP tools, session) lives on the agent, not on the chat client.
//
// Pipeline order (outermost → innermost):
//   0. ContentSafetyChatClient  — blocks unsafe inputs before any model round-trip.
//      Wired only when ContentSafety:Endpoint is configured (graceful-degradation).
//   1. UseChatReducer           — trims history once per turn, BEFORE the function-
//      invocation loop. Runs inside ContentSafety so history trimming is transparent.
//   2. UseFunctionInvocation    — runs the tool-call loop.
//   3. UseOpenTelemetry         — innermost, captures every model round-trip as its
//      own GenAI span (model, latency, token usage).
public sealed class FoundryChatClientFactory : IAdvisorChatClientFactory
{
    public const string TelemetrySourceName = "OrderSphere.Advisory.Agent";

    // Enough recent context for coherent advice while bounding token cost; the full
    // transcript is persisted in advisory-db regardless.
    private const int MaxNonSystemMessages = 20;

    private readonly Lazy<IChatClient?> _client;

    public FoundryChatClientFactory(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _client = new Lazy<IChatClient?>(() =>
        {
            var endpoint = configuration["Foundry:Endpoint"];
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return null;
            }

            var deployment = configuration["Foundry:Deployment"] ?? "gpt-4o-mini";

            // MEAI001: the chat-reducer API is marked experimental in MEAI 10.x. The
            // alternative is a hand-rolled trimming DelegatingChatClient with identical
            // semantics; accepting the experimental surface is the smaller risk.
#pragma warning disable MEAI001
            IChatClient pipeline = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
                .GetChatClient(deployment)
                .AsIChatClient()
                .AsBuilder()
                .UseChatReducer(new MessageCountingChatReducer(MaxNonSystemMessages))
                .UseFunctionInvocation(loggerFactory)
                .UseOpenTelemetry(loggerFactory, TelemetrySourceName)
                .Build();
#pragma warning restore MEAI001

            var safetyEndpoint = configuration["ContentSafety:Endpoint"];
            if (!string.IsNullOrWhiteSpace(safetyEndpoint))
            {
                var threshold = configuration.GetValue<int?>("ContentSafety:SeverityThreshold") ?? 4;
                var safetyClient = new ContentSafetyClient(
                    new Uri(safetyEndpoint), new DefaultAzureCredential());
                pipeline = new ContentSafetyChatClient(
                    pipeline,
                    (text, ct) => IsBlockedBySafetyAsync(safetyClient, text, threshold, ct));
            }

            return pipeline;
        });
    }

    private static async Task<bool> IsBlockedBySafetyAsync(
        ContentSafetyClient safety, string text, int threshold, CancellationToken ct)
    {
        try
        {
            var result = (await safety.AnalyzeTextAsync(new AnalyzeTextOptions(text), ct)).Value;
            return result.CategoriesAnalysis.Any(a => a.Severity >= threshold);
        }
        catch
        {
            return false; // fail open: unavailable safety service must not block requests
        }
    }

    public IChatClient? GetChatClient() => _client.Value;
}
