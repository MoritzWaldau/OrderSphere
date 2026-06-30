using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

public interface IAdminInvoicingClient
{
    Task<ApiResult<AdminInvoiceDto>> GetByNumberAsync(string invoiceNumber, CancellationToken ct = default);

    /// <summary>Relative URL of the inline PDF endpoint for an invoice's order. Admins pass the
    /// owner-or-admin guard, so this resolves for any invoice. The BFF attaches the bearer token
    /// to the proxied GET.</summary>
    string GetPdfUrl(Guid orderId);
}

public sealed class AdminInvoicingClient(HttpClient client) : IAdminInvoicingClient
{
    public Task<ApiResult<AdminInvoiceDto>> GetByNumberAsync(string invoiceNumber, CancellationToken ct = default)
        => client.GetApiAsync<AdminInvoiceDto>(
            $"/api/v1/invoices/by-number/{Uri.EscapeDataString(invoiceNumber)}", ct);

    public string GetPdfUrl(Guid orderId) => $"/api/v1/invoices/by-order/{orderId}/pdf";
}
