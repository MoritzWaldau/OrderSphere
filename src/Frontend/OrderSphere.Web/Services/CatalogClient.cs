using System.Net.Http.Json;
using OrderSphere.BuildingBlocks.Contracts;
using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

public interface ICatalogClient
{
    Task<PagedResult<ProductDto>> GetProductsAsync(CancellationToken ct = default);
    Task<ProductDto?> GetProductBySlugAsync(string slug, CancellationToken ct = default);
    Task<PagedResult<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
}

public sealed class CatalogClient(HttpClient client) : ICatalogClient
{
    private readonly HttpClient _client = client;

    public async Task<PagedResult<ProductDto>> GetProductsAsync(CancellationToken ct = default)
    {
        var result = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("/api/v1/products?page=1&pageSize=10", ct);

        if (result == null)
        {
            return new PagedResult<ProductDto>([], 0, 1, 10);
        }

        return result;
    }

    public async Task<ProductDto?> GetProductBySlugAsync(string slug, CancellationToken ct = default)
        => await _client.GetFromJsonAsync<ProductDto?>($"/api/v1/products/{Uri.EscapeDataString(slug)}", ct);

    public async Task<PagedResult<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var result = await _client.GetFromJsonAsync<PagedResult<CategoryDto>>("/api/v1/categories?page=1&pageSize=10", ct);

        if (result is null)
        {
            return new PagedResult<CategoryDto>([], 0, 1, 10);
        }

        return result;
    }
}
