namespace OrderSphere.Catalog.Application.Abstractions;

/// <summary>
/// Cross-service read against the Ordering boundary, used to gate review creation on a
/// real purchase. Implemented by an HTTP client in Infrastructure; no project reference.
/// </summary>
public interface IOrderingClient
{
    /// <summary>True when the customer has a non-cancelled order containing the product.
    /// Returns false on any transport failure (fail-closed: unverified purchasers cannot review).</summary>
    Task<bool> HasPurchasedAsync(Guid customerId, Guid productId, CancellationToken ct = default);
}
