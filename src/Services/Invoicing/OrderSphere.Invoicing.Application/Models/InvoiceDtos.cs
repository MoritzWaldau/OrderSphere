namespace OrderSphere.Invoicing.Application.Models;

public sealed record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid OrderId,
    string CustomerEmail,
    string CustomerName,
    decimal Total,
    DateTime IssuedAt);

// Admin support/detail view: original (immutable) amounts plus the currently effective amounts
// after adjustments, the adjustment history, and the status derived from the most recent adjustment.
public sealed record InvoiceAdminDto(
    Guid Id,
    string InvoiceNumber,
    Guid OrderId,
    string CustomerName,
    string CustomerEmail,
    decimal Total,
    decimal NetAmount,
    decimal TaxRate,
    decimal TaxAmount,
    decimal AdjustedNet,
    decimal AdjustedTax,
    decimal AdjustedTotal,
    DateTime IssuedAt,
    string Status,
    IReadOnlyList<InvoiceAdjustmentDto> Adjustments);

public sealed record InvoiceAdjustmentDto(
    Guid Id,
    string Type,
    decimal AmountNet,
    string Reason,
    string AppliedBy,
    DateTime AppliedAt);

public sealed record InvoiceCreatedDto(string InvoiceNumber, string PdfUrl);

public sealed record InvoiceItemDto(string ProductName, int Quantity, decimal UnitPrice);

public sealed record InvoicePdfDto(byte[] Content, string FileName);
