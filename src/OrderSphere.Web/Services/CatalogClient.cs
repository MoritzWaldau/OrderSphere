using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using OrderSphere.BuildingBlocks.Contracts;
using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

public interface ICatalogClient
{
    Task<PagedResult<ProductDto>> GetProductsAsync(
        int page = 1, int pageSize = 20,
        string? search = null, Guid? categoryId = null,
        CancellationToken ct = default);
    Task<ProductDto?> GetProductBySlugAsync(string slug, CancellationToken ct = default);
    Task<PagedResult<CategoryDto>> GetCategoriesAsync(int page = 1, int pageSize = 100, CancellationToken ct = default);
}

public sealed class CatalogClient(HttpClient client) : ICatalogClient
{
    private readonly HttpClient _client = client;

    public async Task<PagedResult<ProductDto>> GetProductsAsync(
        int page = 1, int pageSize = 20,
        string? search = null, Guid? categoryId = null,
        CancellationToken ct = default)
    {
        var qs = new System.Text.StringBuilder($"/api/v1/products?page={page}&pageSize={pageSize}");
        if (!string.IsNullOrWhiteSpace(search))
            qs.Append($"&search={Uri.EscapeDataString(search)}");
        if (categoryId.HasValue)
            qs.Append($"&categoryId={categoryId.Value}");

        return await _client.GetFromJsonAsync<PagedResult<ProductDto>>(qs.ToString(), ct)
               ?? new PagedResult<ProductDto>([], 0, page, pageSize);
    }

    public async Task<ProductDto?> GetProductBySlugAsync(string slug, CancellationToken ct = default)
        => await _client.GetFromJsonAsync<ProductDto?>($"/api/v1/products/{Uri.EscapeDataString(slug)}", ct);

    public async Task<PagedResult<CategoryDto>> GetCategoriesAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        return await _client.GetFromJsonAsync<PagedResult<CategoryDto>>($"/api/v1/categories?page={page}&pageSize={pageSize}", ct)
               ?? new PagedResult<CategoryDto>([], 0, page, pageSize);
    }
}
