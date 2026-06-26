using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using OrderSphere.Advisory.Api.Agent;
using Xunit;

namespace OrderSphere.Advisory.Tests;

public sealed class SemanticCacheChatClientTests : IDisposable
{
    private readonly Meter _meter = new("SemanticCacheChatClientTests");
    private readonly HybridCache _cache = CreateHybridCache();

    private static HybridCache CreateHybridCache()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        return services.BuildServiceProvider().GetRequiredService<HybridCache>();
    }

    private SemanticCacheChatClient CreateSut(
        IChatClient inner,
        Func<string, CancellationToken, Task<ReadOnlyMemory<float>>>? embedAsync = null,
        float threshold = 0.92f)
    {
        embedAsync ??= (_, _) => Task.FromResult<ReadOnlyMemory<float>>(new float[4]);
        return new SemanticCacheChatClient(
            inner,
            embedAsync,
            _cache,
            _meter.CreateCounter<long>("hits"),
            _meter.CreateCounter<long>("misses"),
            threshold);
    }

    private static async Task<List<ChatResponseUpdate>> CollectAsync(IChatClient client, string userMessage)
    {
        var messages = new List<ChatMessage> { new(ChatRole.User, userMessage) };
        var updates = new List<ChatResponseUpdate>();
        await foreach (var u in client.GetStreamingResponseAsync(messages))
            updates.Add(u);
        return updates;
    }

    [Fact]
    public async Task FirstCall_DelegatesToInner_AndStreamsThrough()
    {
        var inner = new ScriptedChatClient().AddTextTurn("Hello.");
        var sut = CreateSut(inner);

        var updates = await CollectAsync(sut, "Hallo");

        updates.Should().ContainSingle(u => u.Text == "Hello.");
    }

    [Fact]
    public async Task SecondIdenticalCall_ReturnsCachedResponse()
    {
        // Two calls with identical embeddings (both return [1,0,0,0]) → second is a hit.
        var embedding = new float[] { 1f, 0f, 0f, 0f };
        ReadOnlyMemory<float> embed = embedding;
        var callCount = 0;

        var inner = new ScriptedChatClient()
            .AddTextTurn("Cached answer.")
            .AddTextTurn("Should not be reached.");

        var sut = CreateSut(inner, (_, _) =>
        {
            callCount++;
            return Task.FromResult<ReadOnlyMemory<float>>(embed);
        });

        await CollectAsync(sut, "Question 1");
        var secondResult = await CollectAsync(sut, "Question 2");

        secondResult.Should().ContainSingle(u => u.Text == "Cached answer.");
        callCount.Should().Be(2, because: "embedding is called for both turns, but inner only once");
    }

    [Fact]
    public async Task FunctionCallResponse_IsNotCached()
    {
        // First call returns a function-call update (not cacheable).
        // Second call hits inner again (nothing cached) and returns a plain-text answer.
        var inner = new ScriptedChatClient()
            .AddFunctionCallTurn("id-1", "search_products") // first request: not cached
            .AddTextTurn("Fresh answer."); // second request: should come from inner, not cache

        var embedding = new float[] { 1f, 0f, 0f, 0f };
        var sut = CreateSut(inner, (_, _) => Task.FromResult<ReadOnlyMemory<float>>(new ReadOnlyMemory<float>(embedding)));

        await CollectAsync(sut, "Sneakers");
        var second = await CollectAsync(sut, "Sneakers again");

        // Second call must hit inner (not cache) — function-call turns are never cached.
        second.Should().Contain(u => u.Text == "Fresh answer.");
    }

    [Fact]
    public async Task ConfirmPayload_IsNotCached()
    {
        const string confirmJson =
            """{"__confirm__":"add_to_cart","slug":"sneaker-x","quantity":1}""";

        var inner = new ScriptedChatClient()
            .AddTextTurn(confirmJson)
            .AddTextTurn("Should not be reached.");

        var embedding = new float[] { 1f, 0f, 0f, 0f };
        var sut = CreateSut(inner, (_, _) => Task.FromResult<ReadOnlyMemory<float>>(new ReadOnlyMemory<float>(embedding)));

        await CollectAsync(sut, "Sneaker X kaufen");

        // A second call with identical embedding must hit inner (not cache).
        var innerCallCount = 0;
        var sut2 = CreateSut(
            new ScriptedChatClient().AddTextTurn("New answer."),
            (_, _) =>
            {
                innerCallCount++;
                return Task.FromResult<ReadOnlyMemory<float>>(new ReadOnlyMemory<float>(embedding));
            });

        // Populate sut2's cache with a cacheable response first to ensure the index starts empty.
        await CollectAsync(sut2, "Sneaker X kaufen");
    }

    [Fact]
    public async Task EmbeddingFailure_FailsOpen_AndDelegatesToInner()
    {
        var inner = new ScriptedChatClient().AddTextTurn("Fallback response.");
        var sut = CreateSut(inner, (_, _) => Task.FromException<ReadOnlyMemory<float>>(new HttpRequestException("embedding service down")));

        var updates = await CollectAsync(sut, "any question");

        updates.Should().ContainSingle(u => u.Text == "Fallback response.");
    }

    [Fact]
    public async Task EmptyUserMessage_SkipsCache_AndDelegatesToInner()
    {
        var inner = new ScriptedChatClient().AddTextTurn("System-level response.");
        var checkerCalled = false;
        var sut = CreateSut(inner, (_, _) =>
        {
            checkerCalled = true;
            return Task.FromResult<ReadOnlyMemory<float>>(new float[4]);
        });

        var messages = new List<ChatMessage> { new(ChatRole.System, "Only system message.") };
        var updates = new List<ChatResponseUpdate>();
        await foreach (var u in sut.GetStreamingResponseAsync(messages))
            updates.Add(u);

        checkerCalled.Should().BeFalse();
        updates.Should().ContainSingle(u => u.Text == "System-level response.");
    }

    public void Dispose() => _meter.Dispose();
}
