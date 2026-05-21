using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.Application.Models.Admin;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Infrastructure.CatalogClient;

public sealed class HttpCatalogClient(
    HttpClient httpClient,
    ILogger<HttpCatalogClient> logger) : ICatalogClient
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    // ── Product queries ───────────────────────────────────────────────────────

    public async Task<Result<IEnumerable<ProductDto>>> GetProductsAsync(CancellationToken ct = default)
        => await GetAsync<IEnumerable<ProductDto>>("/api/v1/products", ProductErrors.UnknownError, ct);

    public async Task<Result<ProductDto>> GetProductBySlugAsync(string slug, CancellationToken ct = default)
        => await GetAsync<ProductDto>($"/api/v1/products/{slug}", ProductErrors.ProductNotFoundError, ct);

    public async Task<Result<CatalogProductInfo>> GetProductByIdAsync(Guid productId, CancellationToken ct = default)
    {
        var result = await GetAsync<CatalogApiProductDto>($"/api/v1/products/batch?ids={productId}", ProductErrors.ProductNotFoundError, ct);
        if (result.IsFailure) return Result<CatalogProductInfo>.Failure(result.Error);

        // batch returns IEnumerable; we get the first element
        var list = await GetListAsync<CatalogApiProductDto>($"/api/v1/products/batch?ids={productId}", ct);
        var product = list?.FirstOrDefault(p => p.Id == productId);
        return product is null
            ? Result<CatalogProductInfo>.Failure(ProductErrors.ProductNotFoundError)
            : Result<CatalogProductInfo>.Success(new CatalogProductInfo(product.Id, product.Name, product.Price, product.Stock, product.IsActive));
    }

    public async Task<Result<IReadOnlyDictionary<Guid, string>>> GetProductNamesByIdsAsync(
        IEnumerable<Guid> productIds, CancellationToken ct = default)
    {
        var ids = string.Join(",", productIds);
        if (string.IsNullOrEmpty(ids))
            return Result<IReadOnlyDictionary<Guid, string>>.Success(new Dictionary<Guid, string>());

        var products = await GetListAsync<CatalogApiProductDto>($"/api/v1/products/batch?ids={ids}", ct);
        var dict = products?.ToDictionary(p => p.Id, p => p.Name)
                   ?? new Dictionary<Guid, string>();
        return Result<IReadOnlyDictionary<Guid, string>>.Success(dict);
    }

    // ── Product admin ─────────────────────────────────────────────────────────

    public async Task<Result<IEnumerable<AdminProductDto>>> GetAllProductsAdminAsync(CancellationToken ct = default)
        => await GetAsync<IEnumerable<AdminProductDto>>("/api/v1/admin/products", ProductErrors.UnknownError, ct);

    public async Task<Result<AdminProductDto>> GetProductByIdAdminAsync(Guid productId, CancellationToken ct = default)
        => await GetAsync<AdminProductDto>($"/api/v1/admin/products/{productId}", ProductErrors.ProductNotFoundError, ct);

    public async Task<Result<Guid>> CreateProductAsync(AdminProductInput input, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/v1/admin/products", input, ct);
            if (!response.IsSuccessStatusCode) return Result<Guid>.Failure(ProductErrors.UnknownError);
            var result = await response.Content.ReadFromJsonAsync<IdResponse>(JsonOptions, ct);
            return result is null ? Result<Guid>.Failure(ProductErrors.UnknownError) : Result<Guid>.Success(result.Id);
        }
        catch (Exception ex) { return LogAndFailGuid(ex, "CreateProduct"); }
    }

    public async Task<Result<bool>> UpdateProductAsync(Guid productId, AdminProductInput input, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"/api/v1/admin/products/{productId}", input, ct);
            return response.IsSuccessStatusCode ? Result<bool>.Success(true) : Result<bool>.Failure(ProductErrors.UnknownError);
        }
        catch (Exception ex) { return LogAndFailBool(ex, "UpdateProduct"); }
    }

    public async Task<Result<bool>> DeleteProductAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"/api/v1/admin/products/{productId}", ct);
            return response.IsSuccessStatusCode ? Result<bool>.Success(true) : Result<bool>.Failure(ProductErrors.UnknownError);
        }
        catch (Exception ex) { return LogAndFailBool(ex, "DeleteProduct"); }
    }

    // ── Category queries ──────────────────────────────────────────────────────

    public async Task<Result<IEnumerable<CategoryDto>>> GetCategoriesAsync(CancellationToken ct = default)
        => await GetAsync<IEnumerable<CategoryDto>>("/api/v1/categories", CategoryErrors.UnknownError, ct);

    // ── Category admin ────────────────────────────────────────────────────────

    public async Task<Result<IEnumerable<AdminCategoryDto>>> GetAllCategoriesAdminAsync(CancellationToken ct = default)
        => await GetAsync<IEnumerable<AdminCategoryDto>>("/api/v1/admin/categories", CategoryErrors.UnknownError, ct);

    public async Task<Result<Guid>> CreateCategoryAsync(string name, string description, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/v1/admin/categories", new { name, description }, ct);
            if (!response.IsSuccessStatusCode) return Result<Guid>.Failure(CategoryErrors.UnknownError);
            var result = await response.Content.ReadFromJsonAsync<IdResponse>(JsonOptions, ct);
            return result is null ? Result<Guid>.Failure(CategoryErrors.UnknownError) : Result<Guid>.Success(result.Id);
        }
        catch (Exception ex) { return LogAndFailGuid(ex, "CreateCategory"); }
    }

    public async Task<Result<bool>> UpdateCategoryAsync(Guid categoryId, string name, string description, bool isActive, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"/api/v1/admin/categories/{categoryId}", new { name, description, isActive }, ct);
            return response.IsSuccessStatusCode ? Result<bool>.Success(true) : Result<bool>.Failure(CategoryErrors.UnknownError);
        }
        catch (Exception ex) { return LogAndFailBool(ex, "UpdateCategory"); }
    }

    public async Task<Result<bool>> DeleteCategoryAsync(Guid categoryId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"/api/v1/admin/categories/{categoryId}", ct);
            return response.IsSuccessStatusCode ? Result<bool>.Success(true) : Result<bool>.Failure(CategoryErrors.UnknownError);
        }
        catch (Exception ex) { return LogAndFailBool(ex, "DeleteCategory"); }
    }

    // ── Stock operations ──────────────────────────────────────────────────────

    public async Task<Result> DecrementStockAsync(Guid productId, int quantity, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"/api/v1/products/{productId}/stock/decrement", new { quantity }, ct);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(ProductErrors.InsufficientStockError);
        }
        catch (Exception ex) { return LogAndFail(ex, "DecrementStock"); }
    }

    public async Task<Result> RestoreStockAsync(Guid productId, int quantity, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"/api/v1/products/{productId}/stock/restore", new { quantity }, ct);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(ProductErrors.UnknownError);
        }
        catch (Exception ex) { return LogAndFail(ex, "RestoreStock"); }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Result<T>> GetAsync<T>(string url, Error fallbackError, CancellationToken ct)
    {
        try
        {
            var response = await httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return Result<T>.Failure(fallbackError);
            var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
            return value is null ? Result<T>.Failure(fallbackError) : Result<T>.Success(value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Catalog GET {Url} failed", url);
            return Result<T>.Failure(fallbackError);
        }
    }

    private async Task<IEnumerable<T>?> GetListAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            var response = await httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<IEnumerable<T>>(JsonOptions, ct);
        }
        catch { return null; }
    }

    private Result LogAndFail(Exception ex, string op)
    {
        logger.LogError(ex, "Catalog {Op} failed", op);
        return Result.Failure(ProductErrors.UnknownError);
    }

    private Result<Guid> LogAndFailGuid(Exception ex, string op)
    {
        logger.LogError(ex, "Catalog {Op} failed", op);
        return Result<Guid>.Failure(ProductErrors.UnknownError);
    }

    private Result<bool> LogAndFailBool(Exception ex, string op)
    {
        logger.LogError(ex, "Catalog {Op} failed", op);
        return Result<bool>.Failure(ProductErrors.UnknownError);
    }

    private sealed record CatalogApiProductDto(Guid Id, string Name, string Slug, string Description,
        decimal Price, int Stock, Guid CategoryId, string CategoryName, string SKU, string? ImageUrl, bool IsActive);

    private sealed record IdResponse(Guid Id);
}
