namespace OrderSphere.Catalog.Infrastructure.Search;

/// <summary>
/// No-op index used when Azure AI Search is not configured. Searches return an empty
/// page (the caller falls back to the database) and writes do nothing.
/// </summary>
public sealed class DisabledProductSearchIndex : IProductSearchIndex
{
    public static readonly DisabledProductSearchIndex Instance = new();

    private DisabledProductSearchIndex() { }

    public bool IsEnabled => false;

    public Task SyncAsync(Guid productId, CancellationToken ct) => Task.CompletedTask;

    public Task RemoveAsync(Guid productId, CancellationToken ct) => Task.CompletedTask;

    public Task<ProductSearchPage> SearchAsync(ProductSearchCriteria criteria, CancellationToken ct)
        => Task.FromResult(new ProductSearchPage([], 0));

    public Task EnsureSeededAsync(CancellationToken ct) => Task.CompletedTask;

    public Task<int> ReindexAllAsync(CancellationToken ct) => Task.FromResult(0);

    public Task<IReadOnlyList<Guid>> FindSimilarAsync(string productText, Guid excludeId, int limit, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Guid>>([]);
}
