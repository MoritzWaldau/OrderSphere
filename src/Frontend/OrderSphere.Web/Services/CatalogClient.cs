using OrderSphere.BuildingBlocks.Contracts;
using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

public interface ICatalogClient
{
    Task<ApiResult<PagedResult<ProductDto>>> GetProductsAsync(CancellationToken ct = default);
    Task<ApiResult<ProductDto>> GetProductBySlugAsync(string slug, CancellationToken ct = default);
    Task<ApiResult<PagedResult<CategoryDto>>> GetCategoriesAsync(CancellationToken ct = default);
}

public sealed class CatalogClient(HttpClient client) : ICatalogClient
{
    public Task<ApiResult<PagedResult<ProductDto>>> GetProductsAsync(CancellationToken ct = default)
        => client.GetApiAsync<PagedResult<ProductDto>>("/api/v1/products?page=1&pageSize=10", ct);

    public Task<ApiResult<ProductDto>> GetProductBySlugAsync(string slug, CancellationToken ct = default)
        => client.GetApiAsync<ProductDto>($"/api/v1/products/{Uri.EscapeDataString(slug)}", ct);

    public Task<ApiResult<PagedResult<CategoryDto>>> GetCategoriesAsync(CancellationToken ct = default)
        => client.GetApiAsync<PagedResult<CategoryDto>>("/api/v1/categories?page=1&pageSize=10", ct);
}
