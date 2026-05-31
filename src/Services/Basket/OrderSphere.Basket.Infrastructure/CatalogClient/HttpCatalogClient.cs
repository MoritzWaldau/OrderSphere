using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using OrderSphere.Basket.Application.Abstractions;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Basket.Infrastructure.CatalogClient;

public sealed class HttpCatalogClient(HttpClient httpClient, ILogger<HttpCatalogClient> logger) : ICatalogClient
{
    public async Task<Result<CatalogProductInfo>> GetProductByIdAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"/internal/products/{productId}", ct);
            if (!response.IsSuccessStatusCode)
                return Result<CatalogProductInfo>.Failure(new Error("Catalog.ProductNotFound", "Product not found."));

            var dto = await response.Content.ReadFromJsonAsync<CatalogProductInfo>(ct);
            return dto is not null
                ? Result<CatalogProductInfo>.Success(dto)
                : Result<CatalogProductInfo>.Failure(new Error("Catalog.ProductNotFound", "Product not found."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching product {ProductId} from Catalog", productId);
            return Result<CatalogProductInfo>.Failure(new Error("Catalog.Unavailable", "Catalog service unavailable."));
        }
    }

    public async Task<Result<IReadOnlyDictionary<Guid, CatalogProductInfo>>> GetProductInfosByIdsAsync(
        IEnumerable<Guid> productIds, CancellationToken ct = default)
    {
        try
        {
            var ids = productIds.ToList();
            if (ids.Count == 0)
                return Result<IReadOnlyDictionary<Guid, CatalogProductInfo>>.Success(
                    new Dictionary<Guid, CatalogProductInfo>());

            var query = string.Join("&", ids.Select(id => $"ids={id}"));
            var response = await httpClient.GetAsync($"/internal/products/infos?{query}", ct);

            if (!response.IsSuccessStatusCode)
                return Result<IReadOnlyDictionary<Guid, CatalogProductInfo>>.Success(
                    new Dictionary<Guid, CatalogProductInfo>());

            var dict = await response.Content.ReadFromJsonAsync<Dictionary<Guid, CatalogProductInfo>>(ct);
            return Result<IReadOnlyDictionary<Guid, CatalogProductInfo>>.Success(
                dict ?? new Dictionary<Guid, CatalogProductInfo>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching product infos from Catalog");
            return Result<IReadOnlyDictionary<Guid, CatalogProductInfo>>.Success(
                new Dictionary<Guid, CatalogProductInfo>());
        }
    }
}
