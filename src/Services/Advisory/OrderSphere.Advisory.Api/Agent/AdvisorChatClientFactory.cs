using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Hybrid;
using OpenAI.Embeddings;
using OrderSphere.BuildingBlocks.Diagnostics;

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
//   1. SemanticCacheChatClient — serves identical/similar questions from cache
//      before any model round-trip. Enabled when Foundry:Endpoint is set.
//   2. UseChatReducer          — trims history once per turn, BEFORE the function-
//      invocation loop. Runs inside the cache decorator so trimming is transparent.
//   3. UseFunctionInvocation   — runs the tool-call loop.
//   4. UseOpenTelemetry        — innermost, captures every model round-trip as its
//      own GenAI span (model, latency, token usage).
public sealed class FoundryChatClientFactory : IAdvisorChatClientFactory
{
    public const string TelemetrySourceName = "OrderSphere.Advisory.Agent";

    // Enough recent context for coherent advice while bounding token cost; the full
    // transcript is persisted in advisory-db regardless.
    private const int MaxNonSystemMessages = 20;

    private readonly Lazy<IChatClient?> _client;

    public FoundryChatClientFactory(
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        HybridCache hybridCache)
    {
        _client = new Lazy<IChatClient?>(() =>
        {
            var endpoint = configuration["Foundry:Endpoint"];
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return null;
            }

            var deployment = configuration["Foundry:Deployment"] ?? "gpt-4o-mini";
            var credential = new DefaultAzureCredential();
            var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);

            // MEAI001: the chat-reducer API is marked experimental in MEAI 10.x. The
            // alternative is a hand-rolled trimming DelegatingChatClient with identical
            // semantics; accepting the experimental surface is the smaller risk.
#pragma warning disable MEAI001
            IChatClient pipeline = azureClient
                .GetChatClient(deployment)
                .AsIChatClient()
                .AsBuilder()
                .UseChatReducer(new MessageCountingChatReducer(MaxNonSystemMessages))
                .UseFunctionInvocation(loggerFactory)
                .UseOpenTelemetry(loggerFactory, TelemetrySourceName)
                .Build();
#pragma warning restore MEAI001

            var embeddingDeployment = configuration["Foundry:EmbeddingDeployment"] ?? "text-embedding-3-small";
            var embeddingClient = azureClient.GetEmbeddingClient(embeddingDeployment);
            var threshold = configuration.GetValue<float?>("SemanticCache:Threshold") ?? 0.92f;

            pipeline = new SemanticCacheChatClient(
                pipeline,
                (text, ct) => EmbedAsync(embeddingClient, text, ct),
                hybridCache,
                ApplicationDiagnostics.AdvisorCacheHits,
                ApplicationDiagnostics.AdvisorCacheMisses,
                threshold);

            return pipeline;
        });
    }

    public IChatClient? GetChatClient() => _client.Value;

    private static async Task<ReadOnlyMemory<float>> EmbedAsync(
        EmbeddingClient client, string text, CancellationToken ct)
    {
        var result = await client.GenerateEmbeddingAsync(text, cancellationToken: ct);
        return result.Value.ToFloats();
    }
}
