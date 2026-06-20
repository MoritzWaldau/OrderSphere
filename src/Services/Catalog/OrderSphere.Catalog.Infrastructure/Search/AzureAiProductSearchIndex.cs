using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Catalog.Infrastructure.Search;

/// <summary>
/// Azure AI Search implementation of <see cref="IProductSearchIndex"/>. Scoped, so it
/// can read the catalog through <see cref="ICatalogDbContext"/>; the expensive Azure
/// clients are borrowed from the singleton <see cref="ProductSearchClients"/>.
/// </summary>
public sealed class AzureAiProductSearchIndex(
    ICatalogDbContext context,
    ProductSearchClients clients,
    ILogger<AzureAiProductSearchIndex> logger) : IProductSearchIndex
{
    // KNN candidates pulled before fusion; comfortably covers paged result windows.
    private const int VectorNeighbors = 50;
    private const int UploadBatchSize = 100;

    public bool IsEnabled => clients.IsEnabled;

    public async Task SyncAsync(Guid productId, CancellationToken ct)
    {
        if (!clients.IsEnabled)
            return;

        try
        {
            // The global soft-delete filter excludes deleted rows: a missing product
            // means it should not be searchable, so remove it from the index.
            var product = await context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == ProductId.From(productId), ct);

            if (product is null)
            {
                await RemoveAsync(productId, ct);
                return;
            }

            var document = await BuildDocumentAsync(product, ct);
            await clients.SearchClient!.MergeOrUploadDocumentsAsync(new[] { document }, cancellationToken: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Best-effort: indexing must never break a catalog write.
            logger.LogError(ex, "Failed to sync product {ProductId} to the search index.", productId);
        }
    }

    public async Task RemoveAsync(Guid productId, CancellationToken ct)
    {
        if (!clients.IsEnabled)
            return;

        try
        {
            await clients.SearchClient!.DeleteDocumentsAsync("id", [productId.ToString()], cancellationToken: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to remove product {ProductId} from the search index.", productId);
        }
    }

    public async Task<ProductSearchPage> SearchAsync(ProductSearchCriteria criteria, CancellationToken ct)
    {
        var queryVector = await EmbedAsync(criteria.Text, ct);

        var options = new SearchOptions
        {
            Size = criteria.Take,
            Skip = criteria.Skip,
            IncludeTotalCount = true,
            Filter = BuildFilter(criteria),
            VectorSearch = new VectorSearchOptions
            {
                Queries =
                {
                    new VectorizedQuery(queryVector)
                    {
                        KNearestNeighborsCount = VectorNeighbors,
                        Fields = { ProductSearchClients.VectorFieldName },
                    },
                },
            },
        };
        options.Select.Add("id");

        var response = await clients.SearchClient!.SearchAsync<SearchDocument>(criteria.Text, options, ct);

        var ids = new List<Guid>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            if (result.Document.TryGetValue("id", out var raw)
                && Guid.TryParse(raw?.ToString(), out var id))
            {
                ids.Add(id);
            }
        }

        return new ProductSearchPage(ids, response.Value.TotalCount ?? ids.Count);
    }

    public async Task EnsureSeededAsync(CancellationToken ct)
    {
        if (!clients.IsEnabled)
            return;

        await clients.EnsureIndexAsync(ct);

        var indexed = await clients.SearchClient!.GetDocumentCountAsync(ct);
        if (indexed > 0)
            return;

        await ReindexAllAsync(ct);
    }

    public async Task<int> ReindexAllAsync(CancellationToken ct)
    {
        if (!clients.IsEnabled)
            return 0;

        await clients.EnsureIndexAsync(ct);

        var products = await context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .AsNoTracking()
            .ToListAsync(ct);

        var total = 0;
        foreach (var chunk in products.Chunk(UploadBatchSize))
        {
            var documents = new List<SearchDocument>(chunk.Length);
            foreach (var product in chunk)
                documents.Add(await BuildDocumentAsync(product, ct));

            await clients.SearchClient!.MergeOrUploadDocumentsAsync(documents, cancellationToken: ct);
            total += documents.Count;
        }

        return total;
    }

    private async Task<SearchDocument> BuildDocumentAsync(Product product, CancellationToken ct)
    {
        var categoryName = product.Category?.Name ?? string.Empty;
        var brandName = product.Brand?.Name ?? string.Empty;
        var vector = await EmbedAsync($"{product.Name}\n{brandName}\n{categoryName}\n{product.Description}", ct);

        return new SearchDocument
        {
            ["id"] = product.Id.Value.ToString(),
            ["name"] = product.Name,
            ["description"] = product.Description,
            ["categoryName"] = categoryName,
            ["brandName"] = brandName,
            ["price"] = (double)product.Price.Amount,
            ["isActive"] = product.IsActive,
            [ProductSearchClients.VectorFieldName] = vector,
        };
    }

    private async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        var embedding = await clients.EmbeddingClient!.GenerateEmbeddingAsync(text, cancellationToken: ct);
        return embedding.Value.ToFloats().ToArray();
    }

    // OData filter. Only active products are searchable; category/price narrow further.
    private static string BuildFilter(ProductSearchCriteria criteria)
    {
        var clauses = new List<string> { "isActive eq true" };

        if (!string.IsNullOrWhiteSpace(criteria.CategoryName))
        {
            var escaped = criteria.CategoryName.Replace("'", "''");
            clauses.Add($"categoryName eq '{escaped}'");
        }

        if (criteria.MinPrice is { } min)
            clauses.Add($"price ge {min.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        if (criteria.MaxPrice is { } max)
            clauses.Add($"price le {max.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        return string.Join(" and ", clauses);
    }
}
