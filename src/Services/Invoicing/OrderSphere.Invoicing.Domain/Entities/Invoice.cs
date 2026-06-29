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

    // The sequential invoice number and issue timestamp are allocated by the application layer
    // (see IInvoiceNumberGenerator) and passed in, so the domain constructor stays pure and
    // deterministic — no clock or counter access here.
    public static Invoice Create(
        Guid orderId,
        string customerEmail,
        string customerName,
        decimal total,
        IReadOnlyList<InvoiceLineItem> items,
        string invoiceNumber,
        DateTime issuedAt)
    {
        return new Invoice
        {
            Id = InvoiceId.New(),
            InvoiceNumber = invoiceNumber,
            OrderId = orderId,
            CustomerEmail = customerEmail,
            CustomerName = customerName,
            Total = total,
            IssuedAt = issuedAt,
            Items = [.. items],
        };
    }

    public void SetBlobPath(string blobPath) => BlobPath = blobPath;
}
