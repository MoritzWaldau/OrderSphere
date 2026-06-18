using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Ordering.Application.Abstractions;

public interface ICatalogClient
{
    Task<Result<CatalogProductInfo>> GetProductByIdAsync(Guid productId, CancellationToken ct = default);
    Task<Result<IReadOnlyDictionary<Guid, string>>> GetProductNamesByIdsAsync(IEnumerable<Guid> productIds, CancellationToken ct = default);
    Task<Result> DecrementStockAsync(Guid productId, int quantity, CancellationToken ct = default);
    Task<Result> RestoreStockAsync(Guid productId, int quantity, CancellationToken ct = default);

    /// <summary>Reserves stock for a checkout against its correlation id. Fails (Conflict) when
    /// availability is insufficient. Idempotent for a correlation id already holding a reservation.</summary>
    Task<Result> ReserveStockAsync(Guid correlationId, IReadOnlyList<ReservationItem> items, CancellationToken ct = default);

    /// <summary>Confirms a reservation on payment success — decrements on-hand stock. Idempotent.</summary>
    Task<Result> ConfirmReservationAsync(Guid correlationId, CancellationToken ct = default);

    /// <summary>Releases a reservation on payment failure/cancellation. Idempotent.</summary>
    Task<Result> ReleaseReservationAsync(Guid correlationId, CancellationToken ct = default);
}

public sealed record CatalogProductInfo(Guid Id, string Name, decimal Price, int Stock, bool IsActive);

public sealed record ReservationItem(Guid ProductId, int Quantity);
