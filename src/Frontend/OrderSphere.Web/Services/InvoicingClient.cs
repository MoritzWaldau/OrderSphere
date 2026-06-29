using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

public interface IInvoicingClient
{
    Task<ApiResult<InvoiceDto>> GetByOrderAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>Relative URL of the inline PDF endpoint, suitable for an anchor/new-tab link.
    /// The BFF attaches the bearer token to the proxied GET, so no extra auth handling is needed.</summary>
    string GetPdfUrl(Guid orderId);

    /// <summary>Relative URL of the PDF endpoint with attachment disposition, so a plain anchor
    /// triggers a file download rather than rendering inline.</summary>
    string GetDownloadUrl(Guid orderId);
}

public sealed class InvoicingClient(HttpClient client) : IInvoicingClient
{
    public Task<ApiResult<InvoiceDto>> GetByOrderAsync(Guid orderId, CancellationToken ct = default)
        => client.GetApiAsync<InvoiceDto>($"/api/v1/invoices/by-order/{orderId}", ct);

    public string GetPdfUrl(Guid orderId) => $"/api/v1/invoices/by-order/{orderId}/pdf";

    public string GetDownloadUrl(Guid orderId) => $"/api/v1/invoices/by-order/{orderId}/pdf?download=true";
}
