namespace OrderSphere.Invoicing.Application.Models;

public sealed record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid OrderId,
    string CustomerEmail,
    string CustomerName,
    decimal Total,
    DateTime IssuedAt);

public sealed record InvoiceCreatedDto(string InvoiceNumber, string PdfUrl);

public sealed record InvoiceItemDto(string ProductName, int Quantity, decimal UnitPrice);

public sealed record InvoicePdfDto(byte[] Content, string FileName);
