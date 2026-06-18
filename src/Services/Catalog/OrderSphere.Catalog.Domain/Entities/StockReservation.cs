using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Catalog.Domain.Enums;

namespace OrderSphere.Catalog.Domain.Entities;

/// <summary>
/// A hold on stock for one product, taken at checkout against a checkout correlation id.
/// One row per reserved product. Availability for a product is its on-hand
/// <see cref="Product.Stock"/> minus the quantity of all non-expired <see cref="ReservationStatus.Active"/>
/// reservations. Confirmed on payment success (decrements on-hand stock), released on
/// payment failure / cancellation, or swept to <see cref="ReservationStatus.Released"/> after expiry.
/// </summary>
public sealed class StockReservation : AuditableEntity<ReservationId>, IAggregateRoot
{
    public Guid CorrelationId { get; private set; }
    public ProductId ProductId { get; private set; }
    public int Quantity { get; private set; }
    public ReservationStatus Status { get; private set; } = ReservationStatus.Active;
    public DateTime ExpiresAt { get; private set; }

    // Parameterless constructor for EF Core materialisation.
    private StockReservation() { }

    public StockReservation(Guid correlationId, ProductId productId, int quantity, DateTime expiresAt)
    {
        Id = ReservationId.New();
        CorrelationId = correlationId;
        ProductId = productId;
        Quantity = quantity;
        Status = ReservationStatus.Active;
        ExpiresAt = expiresAt;
    }

    public bool IsActive(DateTime nowUtc) => Status == ReservationStatus.Active && ExpiresAt > nowUtc;

    public void Confirm() => Status = ReservationStatus.Confirmed;

    public void Release() => Status = ReservationStatus.Released;
}
