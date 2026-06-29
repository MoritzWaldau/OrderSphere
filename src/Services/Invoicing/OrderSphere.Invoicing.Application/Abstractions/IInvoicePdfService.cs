namespace OrderSphere.Invoicing.Application.Abstractions;

public interface IInvoicePdfService
{
    Task<byte[]> GenerateAsync(Invoice invoice, CancellationToken ct = default);
}
