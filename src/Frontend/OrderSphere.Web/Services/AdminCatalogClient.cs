using System.Net.Http.Json;
using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

public interface IAdminCatalogClient
{
    Task<ApiResult<List<AdminProductDto>>> GetProductsAsync(CancellationToken ct = default);
    Task<ApiResult<AdminProductDto>> GetProductByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApiResult<Guid>> CreateProductAsync(AdminProductInput input, CancellationToken ct = default);
    Task<ApiResult> UpdateProductAsync(Guid id, AdminProductInput input, CancellationToken ct = default);
    Task<ApiResult> DeleteProductAsync(Guid id, CancellationToken ct = default);
    Task<ApiResult<List<AdminCategoryDto>>> GetCategoriesAsync(CancellationToken ct = default);
    Task<ApiResult<Guid>> CreateCategoryAsync(AdminCategoryInput input, CancellationToken ct = default);
    Task<ApiResult> UpdateCategoryAsync(Guid id, AdminCategoryInput input, CancellationToken ct = default);
    Task<ApiResult> DeleteCategoryAsync(Guid id, CancellationToken ct = default);
    Task<ApiResult<List<ReviewDto>>> GetReviewsAsync(CancellationToken ct = default);
    Task<ApiResult> ModerateReviewAsync(Guid reviewId, bool approve, CancellationToken ct = default);
}

public sealed class AdminCatalogClient(HttpClient client) : IAdminCatalogClient
{
    public Task<ApiResult<List<AdminProductDto>>> GetProductsAsync(CancellationToken ct = default)
        => client.GetApiAsync<List<AdminProductDto>>("/api/v1/admin/products", ct);

    public Task<ApiResult<AdminProductDto>> GetProductByIdAsync(Guid id, CancellationToken ct = default)
        => client.GetApiAsync<AdminProductDto>($"/api/v1/admin/products/{id}", ct);

    public async Task<ApiResult<Guid>> CreateProductAsync(AdminProductInput input, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/products") { Content = JsonContent.Create(input) };
        var result = await client.SendApiAsync<CreatedResult>(request, ct);
        return result.IsSuccess ? ApiResult<Guid>.Ok(result.Value!.Id) : ApiResult<Guid>.Fail(result.Error!);
    }

    public Task<ApiResult> UpdateProductAsync(Guid id, AdminProductInput input, CancellationToken ct = default)
        => client.SendApiAsync(
            new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/products/{id}") { Content = JsonContent.Create(input) }, ct);

    public Task<ApiResult> DeleteProductAsync(Guid id, CancellationToken ct = default)
        => client.SendApiAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/admin/products/{id}"), ct);

    public Task<ApiResult<List<AdminCategoryDto>>> GetCategoriesAsync(CancellationToken ct = default)
        => client.GetApiAsync<List<AdminCategoryDto>>("/api/v1/admin/categories", ct);

    public async Task<ApiResult<Guid>> CreateCategoryAsync(AdminCategoryInput input, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/categories") { Content = JsonContent.Create(input) };
        var result = await client.SendApiAsync<CreatedResult>(request, ct);
        return result.IsSuccess ? ApiResult<Guid>.Ok(result.Value!.Id) : ApiResult<Guid>.Fail(result.Error!);
    }

    public Task<ApiResult> UpdateCategoryAsync(Guid id, AdminCategoryInput input, CancellationToken ct = default)
        => client.SendApiAsync(
            new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/categories/{id}") { Content = JsonContent.Create(input) }, ct);

    public Task<ApiResult> DeleteCategoryAsync(Guid id, CancellationToken ct = default)
        => client.SendApiAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/admin/categories/{id}"), ct);

    public Task<ApiResult<List<ReviewDto>>> GetReviewsAsync(CancellationToken ct = default)
        => client.GetApiAsync<List<ReviewDto>>("/api/v1/admin/reviews", ct);

    public Task<ApiResult> ModerateReviewAsync(Guid reviewId, bool approve, CancellationToken ct = default)
        => client.SendApiAsync(
            new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/reviews/{reviewId}/moderate")
            {
                Content = JsonContent.Create(new { Approve = approve }),
            }, ct);

    private sealed record CreatedResult(Guid Id);
}
