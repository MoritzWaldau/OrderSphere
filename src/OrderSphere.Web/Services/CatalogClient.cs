using System.Net.Http.Json;
using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

public interface ICatalogClient
{
    Task<List<ProductDto>> GetProductsAsync(CancellationToken ct = default);
    Task<ProductDto?> GetProductBySlugAsync(string slug, CancellationToken ct = default);
    Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
}

public sealed class CatalogClient : ICatalogClient
{
    private readonly HttpClient _client;

    public CatalogClient(HttpClient client) => _client = client;

    public async Task<List<ProductDto>> GetProductsAsync(CancellationToken ct = default)
    {
        var result = await _client.GetFromJsonAsync<List<ProductDto>>("/api/v1/products", ct);
        return result ?? [];
    }

    public async Task<ProductDto?> GetProductBySlugAsync(string slug, CancellationToken ct = default)
        => await _client.GetFromJsonAsync<ProductDto?>($"/api/v1/products/{Uri.EscapeDataString(slug)}", ct);

    public async Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var result = await _client.GetFromJsonAsync<List<CategoryDto>>("/api/v1/categories", ct);
        return result ?? [];
    }
}
