using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Basket.Application.Abstractions;

public interface ICatalogClient
{
    Task<Result<CatalogProductInfo>> GetProductByIdAsync(Guid productId, CancellationToken ct = default);
    Task<Result<IReadOnlyDictionary<Guid, string>>> GetProductNamesByIdsAsync(IEnumerable<Guid> productIds, CancellationToken ct = default);
}

public sealed record CatalogProductInfo(Guid Id, string Name, decimal Price, int Stock, bool IsActive);
