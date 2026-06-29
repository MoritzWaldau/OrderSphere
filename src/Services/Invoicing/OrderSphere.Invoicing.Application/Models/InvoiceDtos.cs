namespace OrderSphere.Invoicing.Application.Models;

public sealed record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid OrderId,
    string CustomerEmail,
    string CustomerName,
    decimal Total,
    DateTime IssuedAt);

// Admin support view: the same invoice metadata as InvoiceDto plus a Status field. Status is a
// plain string so the admin surface is forward-compatible with I4 (Issued/Adjusted/CreditIssued);
// until the status model lands it is reported as the constant "Issued".
public sealed record InvoiceAdminDto(
    Guid Id,
    string InvoiceNumber,
    Guid OrderId,
    string CustomerName,
    string CustomerEmail,
    decimal Total,
    DateTime IssuedAt,
    string Status);

public sealed record InvoiceCreatedDto(string InvoiceNumber, string PdfUrl);

public sealed record InvoiceItemDto(string ProductName, int Quantity, decimal UnitPrice);

public sealed record InvoicePdfDto(byte[] Content, string FileName);
