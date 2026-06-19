using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Configuration;
using OpenAI.Embeddings;

namespace OrderSphere.Catalog.Infrastructure.Search;

/// <summary>
/// Singleton holder for the Azure AI Search and Azure OpenAI clients. These are
/// expensive to construct and thread-safe, so they are built once and shared. The
/// scoped <see cref="AzureAiProductSearchIndex"/> borrows them per request.
/// </summary>
/// <remarks>
/// Authentication uses <see cref="DefaultAzureCredential"/> (managed identity in
/// Azure, developer credentials locally) — no API keys. The index is enabled only
/// when both the Search endpoint and the Foundry embedding endpoint are configured;
/// otherwise the catalog degrades to database search.
/// </remarks>
public sealed class ProductSearchClients
{
    // text-embedding-3-small produces 1536-dimensional vectors.
    public const int EmbeddingDimensions = 1536;

    private const string VectorProfile = "products-vector-profile";
    private const string HnswConfig = "products-hnsw";
    private const string VectorField = "contentVector";

    public bool IsEnabled { get; }
    public string IndexName { get; }
    public SearchClient? SearchClient { get; }
    public EmbeddingClient? EmbeddingClient { get; }

    private readonly SearchIndexClient? _indexClient;

    public ProductSearchClients(IConfiguration configuration)
    {
        // The Aspire AddAzureSearch reference injects ConnectionStrings:search in Azure;
        // local/dev runs pass Search:Endpoint (empty when Azure is not wired).
        var searchEndpoint = configuration["Search:Endpoint"]
            ?? configuration.GetConnectionString("search");
        var foundryEndpoint = configuration["Foundry:Endpoint"];
        var embeddingDeployment = configuration["Foundry:EmbeddingDeployment"] ?? "text-embedding-3-small";
        IndexName = configuration["Search:IndexName"] ?? "products";

        if (string.IsNullOrWhiteSpace(searchEndpoint) || string.IsNullOrWhiteSpace(foundryEndpoint))
        {
            IsEnabled = false;
            return;
        }

        var credential = new DefaultAzureCredential();
        _indexClient = new SearchIndexClient(new Uri(searchEndpoint), credential);
        SearchClient = _indexClient.GetSearchClient(IndexName);
        EmbeddingClient = new AzureOpenAIClient(new Uri(foundryEndpoint), credential)
            .GetEmbeddingClient(embeddingDeployment);
        IsEnabled = true;
    }

    /// <summary>Vector field name used in queries and documents.</summary>
    public static string VectorFieldName => VectorField;

    /// <summary>Creates or updates the product index definition (idempotent).</summary>
    public async Task EnsureIndexAsync(CancellationToken ct)
    {
        if (!IsEnabled)
            return;

        var index = new SearchIndex(IndexName)
        {
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                new SearchableField("name") { IsFilterable = true, IsSortable = true },
                new SearchableField("description"),
                new SimpleField("categoryName", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SimpleField("price", SearchFieldDataType.Double) { IsFilterable = true, IsSortable = true },
                new SimpleField("isActive", SearchFieldDataType.Boolean) { IsFilterable = true },
                new SearchField(VectorField, SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = EmbeddingDimensions,
                    VectorSearchProfileName = VectorProfile,
                },
            },
            VectorSearch = new VectorSearch
            {
                Profiles = { new VectorSearchProfile(VectorProfile, HnswConfig) },
                Algorithms = { new HnswAlgorithmConfiguration(HnswConfig) },
            },
        };

        await _indexClient!.CreateOrUpdateIndexAsync(index, cancellationToken: ct);
    }
}
