using System.Net.Http.Json;
using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

public interface IAdminCatalogClient
{
    Task<List<AdminProductDto>> GetProductsAsync(CancellationToken ct = default);
    Task<AdminProductDto?> GetProductByIdAsync(Guid id, CancellationToken ct = default);
    Task<Guid?> CreateProductAsync(AdminProductInput input, CancellationToken ct = default);
    Task<bool> UpdateProductAsync(Guid id, AdminProductInput input, CancellationToken ct = default);
    Task<bool> DeleteProductAsync(Guid id, CancellationToken ct = default);
    Task<List<AdminCategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
    Task<Guid?> CreateCategoryAsync(AdminCategoryInput input, CancellationToken ct = default);
    Task<bool> UpdateCategoryAsync(Guid id, AdminCategoryInput input, CancellationToken ct = default);
    Task<bool> DeleteCategoryAsync(Guid id, CancellationToken ct = default);
}

public sealed class AdminCatalogClient : IAdminCatalogClient
{
    private readonly HttpClient _client;
    public AdminCatalogClient(HttpClient client) => _client = client;

    public async Task<List<AdminProductDto>> GetProductsAsync(CancellationToken ct = default)
    {
        var result = await _client.GetFromJsonAsync<List<AdminProductDto>>("/api/v1/admin/products", ct);
        return result ?? [];
    }

    public async Task<AdminProductDto?> GetProductByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _client.GetAsync($"/api/v1/admin/products/{id}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AdminProductDto>(ct);
    }

    public async Task<Guid?> CreateProductAsync(AdminProductInput input, CancellationToken ct = default)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/admin/products", input, ct);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<CreatedResult>(ct);
        return result?.Id;
    }

    public async Task<bool> UpdateProductAsync(Guid id, AdminProductInput input, CancellationToken ct = default)
    {
        var response = await _client.PutAsJsonAsync($"/api/v1/admin/products/{id}", input, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteProductAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _client.DeleteAsync($"/api/v1/admin/products/{id}", ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<AdminCategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var result = await _client.GetFromJsonAsync<List<AdminCategoryDto>>("/api/v1/admin/categories", ct);
        return result ?? [];
    }

    public async Task<Guid?> CreateCategoryAsync(AdminCategoryInput input, CancellationToken ct = default)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/admin/categories", input, ct);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<CreatedResult>(ct);
        return result?.Id;
    }

    public async Task<bool> UpdateCategoryAsync(Guid id, AdminCategoryInput input, CancellationToken ct = default)
    {
        var response = await _client.PutAsJsonAsync($"/api/v1/admin/categories/{id}", input, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteCategoryAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _client.DeleteAsync($"/api/v1/admin/categories/{id}", ct);
        return response.IsSuccessStatusCode;
    }

    private sealed record CreatedResult(Guid Id);
}
