using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Hybrid;

namespace OrderSphere.Advisory.Api.Agent;

/// <summary>
/// DelegatingChatClient decorator that caches plain-text advisory answers by
/// semantic similarity of the user's question. On a cache miss the inner pipeline
/// is called and the response is streamed through to the caller without added
/// latency; the answer is stored asynchronously after the stream finishes.
///
/// Only cacheable turns are stored: responses that contain function calls or a
/// human-in-the-loop confirmation payload are never cached.
///
/// Similarity is computed with cosine distance against a bounded in-memory
/// embedding index (max 200 entries). The actual response text is persisted in
/// HybridCache (L1=in-process, L2=Redis) with a 1-hour TTL. If the embedding
/// service is unavailable the decorator fails open and always delegates.
/// </summary>
public sealed class SemanticCacheChatClient : DelegatingChatClient
{
    private readonly Func<string, CancellationToken, Task<ReadOnlyMemory<float>>> _embedAsync;
    private readonly HybridCache _cache;
    private readonly Counter<long> _hits;
    private readonly Counter<long> _misses;
    private readonly float _threshold;

    // Bounded in-memory index: (embedding vector, HybridCache key).
    private readonly List<(float[] Embedding, string Key)> _index = [];
    private readonly object _indexLock = new();
    private const int MaxIndexEntries = 200;

    private static readonly HybridCacheEntryOptions CacheOptions = new() { Expiration = TimeSpan.FromHours(1) };

    public SemanticCacheChatClient(
        IChatClient innerClient,
        Func<string, CancellationToken, Task<ReadOnlyMemory<float>>> embedAsync,
        HybridCache cache,
        Counter<long> cacheHits,
        Counter<long> cacheMisses,
        float threshold = 0.92f)
        : base(innerClient)
    {
        _embedAsync = embedAsync;
        _cache = cache;
        _hits = cacheHits;
        _misses = cacheMisses;
        _threshold = threshold;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var userText = messages
            .LastOrDefault(m => m.Role == ChatRole.User)
            ?.Text;

        if (string.IsNullOrWhiteSpace(userText))
        {
            await foreach (var u in base.GetStreamingResponseAsync(messages, options, cancellationToken))
                yield return u;
            yield break;
        }

        ReadOnlyMemory<float>? queryEmbedding = null;
        try
        {
            queryEmbedding = await _embedAsync(userText, cancellationToken);
        }
        catch
        {
            // Embedding unavailable — fail open, bypass cache.
        }

        if (queryEmbedding is null)
        {
            await foreach (var u in base.GetStreamingResponseAsync(messages, options, cancellationToken))
                yield return u;
            yield break;
        }

        // Check the in-memory index for a semantically similar cached entry.
        string? hitKey;
        lock (_indexLock)
        {
            hitKey = FindSimilarKey(queryEmbedding.Value.Span);
        }

        if (hitKey is not null)
        {
            var cached = await _cache.GetOrCreateAsync<string?>(
                hitKey,
                _ => ValueTask.FromResult<string?>(null),
                cancellationToken: cancellationToken);

            if (cached is not null)
            {
                _hits.Add(1);
                yield return new ChatResponseUpdate(ChatRole.Assistant, cached);
                yield break;
            }
            // Entry evicted from cache (TTL expired) — treat as miss.
        }

        _misses.Add(1);

        // Miss: stream through the inner pipeline while accumulating the response.
        var textBuffer = new StringBuilder();
        var hasFunctionCall = false;

        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            if (update.Contents.OfType<FunctionCallContent>().Any())
                hasFunctionCall = true;
            if (update.Text is not null)
                textBuffer.Append(update.Text);
            yield return update; // stream through immediately — no added latency
        }

        if (hasFunctionCall)
            yield break;

        var responseText = textBuffer.ToString();
        if (responseText.Length == 0
            || responseText.TrimStart().StartsWith("{\"__confirm__\"", StringComparison.Ordinal))
        {
            yield break;
        }

        // Cacheable plain-text response: store and register in the index.
        var newKey = $"advisor:semantic:{Guid.NewGuid():N}";
        await _cache.SetAsync(newKey, responseText, CacheOptions, cancellationToken: CancellationToken.None);

        var embeddingArray = queryEmbedding.Value.ToArray();
        lock (_indexLock)
        {
            if (_index.Count >= MaxIndexEntries)
                _index.RemoveAt(0); // evict oldest
            _index.Add((embeddingArray, newKey));
        }
    }

    private string? FindSimilarKey(ReadOnlySpan<float> query)
    {
        string? bestKey = null;
        float bestScore = _threshold;

        foreach (var (emb, key) in _index)
        {
            var score = CosineSimilarity(query, emb);
            if (score >= bestScore)
            {
                bestScore = score;
                bestKey = key;
            }
        }

        return bestKey;
    }

    private static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        double dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * (double)a[i];
            magB += b[i] * (double)b[i];
        }

        return (float)(dot / (Math.Sqrt(magA) * Math.Sqrt(magB) + 1e-10));
    }
}
