namespace OrderSphere.Catalog.Application.Abstractions;

/// <summary>
/// Provider-agnostic abstraction over the external product search index
/// (Azure AI Search). The catalog keeps PostgreSQL as the system of record; the
/// index is a derived, best-effort read model used for relevance ranking and
/// hybrid (keyword + vector) retrieval.
/// </summary>
/// <remarks>
/// When the index is not configured, <see cref="IsEnabled"/> is <c>false</c> and a
/// no-op implementation is registered, so queries fall back to a database
/// <c>LIKE</c> search and writes proceed without indexing. This mirrors the
/// graceful-degradation pattern used by the advisory agent for Foundry.
/// </remarks>
public interface IProductSearchIndex
{
    /// <summary>True when an external search index is configured and usable.</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Upserts the current state of a product into the index. Best-effort: a search
    /// outage is logged and swallowed so it never breaks a catalog write.
    /// If the product no longer exists (soft-deleted), it is removed from the index.
    /// </summary>
    Task SyncAsync(Guid productId, CancellationToken ct);

    /// <summary>Removes a product from the index. Best-effort, like <see cref="SyncAsync"/>.</summary>
    Task RemoveAsync(Guid productId, CancellationToken ct);

    /// <summary>
    /// Runs a hybrid (keyword + vector) search and returns the matching product ids
    /// for the requested page, ordered by relevance, together with the total count.
    /// Throws on failure so the caller can fall back to the database.
    /// </summary>
    Task<ProductSearchPage> SearchAsync(ProductSearchCriteria criteria, CancellationToken ct);

    /// <summary>Ensures the index exists and seeds it from the database only when empty (startup).</summary>
    Task EnsureSeededAsync(CancellationToken ct);

    /// <summary>Ensures the index exists and reindexes every product. Returns the number indexed.</summary>
    Task<int> ReindexAllAsync(CancellationToken ct);

    /// <summary>
    /// Returns the ids of up to <paramref name="limit"/> products most similar to
    /// <paramref name="productText"/>, excluding <paramref name="excludeId"/>.
    /// Returns an empty list when the index is not enabled or on failure.
    /// </summary>
    Task<IReadOnlyList<Guid>> FindSimilarAsync(string productText, Guid excludeId, int limit, CancellationToken ct);
}

/// <summary>Inputs for a product search. Filters are applied inside the index.</summary>
public sealed record ProductSearchCriteria(
    string Text,
    string? CategoryName,
    decimal? MinPrice,
    decimal? MaxPrice,
    int Skip,
    int Take);

/// <summary>A relevance-ordered page of product ids plus the total number of matches.</summary>
public sealed record ProductSearchPage(IReadOnlyList<Guid> ProductIds, long Total);
