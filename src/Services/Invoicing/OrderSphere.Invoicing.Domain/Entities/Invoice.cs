using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Invoicing.Domain.Entities;

public sealed class Invoice : AuditableEntity<InvoiceId>
{
    public string InvoiceNumber { get; private set; } = default!;
    public Guid OrderId { get; private set; }
    public string CustomerEmail { get; private set; } = default!;
    public string CustomerName { get; private set; } = default!;
    public decimal Total { get; private set; }
    public string BlobPath { get; private set; } = string.Empty;
    public DateTime IssuedAt { get; private set; }
    public List<InvoiceLineItem> Items { get; private set; } = [];

    private Invoice() { }

    public static Invoice Create(
        Guid orderId,
        string customerEmail,
        string customerName,
        decimal total,
        IReadOnlyList<InvoiceLineItem> items)
    {
        var id = InvoiceId.New();
        var issuedAt = DateTime.UtcNow;
        return new Invoice
        {
            Id = id,
            InvoiceNumber = GenerateNumber(id, issuedAt),
            OrderId = orderId,
            CustomerEmail = customerEmail,
            CustomerName = customerName,
            Total = total,
            IssuedAt = issuedAt,
            Items = [.. items],
        };
    }

    public void SetBlobPath(string blobPath) => BlobPath = blobPath;

    private static string GenerateNumber(InvoiceId id, DateTime issuedAt)
        => $"INV-{issuedAt:yyyy}-{id.Value.ToString("N")[..8].ToUpper()}";
}
