using System.Globalization;
using System.Net.Http.Json;
using OrderSphere.BuildingBlocks.Contracts;
using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

/// <summary>Query parameters for the public product listing. Mirrors the Catalog
/// <c>GetProducts</c> endpoint so search, filtering, sorting and paging happen server-side.</summary>
public sealed record ProductQuery(
    int Page = 1,
    int PageSize = 12,
    string? Search = null,
    string? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? SortBy = null,   // "name" | "price" | "newest"
    string? SortDir = null); // "asc" | "desc"

public interface ICatalogClient
{
    Task<ApiResult<PagedResult<ProductDto>>> GetProductsAsync(ProductQuery? query = null, CancellationToken ct = default);
    Task<ApiResult<ProductDto>> GetProductBySlugAsync(string slug, CancellationToken ct = default);
    Task<ApiResult<PagedResult<CategoryDto>>> GetCategoriesAsync(CancellationToken ct = default);
    Task<ApiResult<List<ReviewDto>>> GetReviewsAsync(Guid productId, CancellationToken ct = default);
    Task<ApiResult> CreateReviewAsync(Guid productId, CreateReviewRequest request, CancellationToken ct = default);
}

public sealed class CatalogClient(HttpClient client) : ICatalogClient
{
    public Task<ApiResult<PagedResult<ProductDto>>> GetProductsAsync(ProductQuery? query = null, CancellationToken ct = default)
    {
        var q = query ?? new ProductQuery();

        var parts = new List<string>
        {
            $"page={q.Page}",
            $"pageSize={q.PageSize}",
        };

        if (!string.IsNullOrWhiteSpace(q.Search))
            parts.Add($"searchTerm={Uri.EscapeDataString(q.Search)}");
        if (!string.IsNullOrWhiteSpace(q.Category))
            parts.Add($"categoryName={Uri.EscapeDataString(q.Category)}");
        if (q.MinPrice is { } min)
            parts.Add($"minPrice={min.ToString(CultureInfo.InvariantCulture)}");
        if (q.MaxPrice is { } max)
            parts.Add($"maxPrice={max.ToString(CultureInfo.InvariantCulture)}");
        if (!string.IsNullOrWhiteSpace(q.SortBy))
            parts.Add($"sortBy={q.SortBy}");
        if (!string.IsNullOrWhiteSpace(q.SortDir))
            parts.Add($"sortDir={q.SortDir}");

        return client.GetApiAsync<PagedResult<ProductDto>>($"/api/v1/products?{string.Join('&', parts)}", ct);
    }

    public Task<ApiResult<ProductDto>> GetProductBySlugAsync(string slug, CancellationToken ct = default)
        => client.GetApiAsync<ProductDto>($"/api/v1/products/{Uri.EscapeDataString(slug)}", ct);

    public Task<ApiResult<PagedResult<CategoryDto>>> GetCategoriesAsync(CancellationToken ct = default)
        => client.GetApiAsync<PagedResult<CategoryDto>>("/api/v1/categories?page=1&pageSize=100", ct);

    public Task<ApiResult<List<ReviewDto>>> GetReviewsAsync(Guid productId, CancellationToken ct = default)
        => client.GetApiAsync<List<ReviewDto>>($"/api/v1/reviews/product/{productId}", ct);

    public Task<ApiResult> CreateReviewAsync(Guid productId, CreateReviewRequest request, CancellationToken ct = default)
        => client.SendApiAsync(
            new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reviews/product/{productId}")
            {
                Content = JsonContent.Create(request),
            }, ct);
}
