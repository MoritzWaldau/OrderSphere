using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Ordering.Domain.Entities;

/// <summary>
/// A single line of a <see cref="ReturnRequest"/>: which product is being returned and how many.
/// Owned by the return aggregate (no independent lifecycle); persisted as an owned collection.
/// The unit price is captured at request time so the refund amount is reproducible even if the
/// catalog price later changes.
/// </summary>
public sealed class ReturnItem
{
    /// <summary>Client-generated surrogate key (provider-agnostic; not a store identity column).</summary>
    public Guid Id { get; private set; }
    public ProductId ProductId { get; private set; }
    public string ProductName { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    private ReturnItem()
    {
        ProductId = ProductId.Empty;
        ProductName = string.Empty;
    }

    public ReturnItem(ProductId productId, string productName, int quantity, decimal unitPrice)
    {
        Id = Guid.CreateVersion7();
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public decimal LineTotal => UnitPrice * Quantity;
}
