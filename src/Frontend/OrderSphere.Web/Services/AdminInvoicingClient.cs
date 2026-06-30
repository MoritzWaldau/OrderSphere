using System.Net.Http.Json;
using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

public interface IAdminInvoicingClient
{
    Task<ApiResult<AdminInvoiceDto>> GetByNumberAsync(string invoiceNumber, CancellationToken ct = default);
    Task<ApiResult<AdminInvoiceDto>> GetByIdAsync(Guid invoiceId, CancellationToken ct = default);
    Task<ApiResult<AdminInvoiceDto>> ApplyDiscountAsync(
        Guid invoiceId, decimal? absoluteAmount, decimal? percentageValue, string reason, CancellationToken ct = default);
    Task<ApiResult<AdminInvoiceDto>> IssueCreditNoteAsync(
        Guid invoiceId, decimal amountNet, string reason, CancellationToken ct = default);

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

    public Task<ApiResult<AdminInvoiceDto>> GetByIdAsync(Guid invoiceId, CancellationToken ct = default)
        => client.GetApiAsync<AdminInvoiceDto>($"/api/v1/invoices/by-id/{invoiceId}", ct);

    public Task<ApiResult<AdminInvoiceDto>> ApplyDiscountAsync(
        Guid invoiceId, decimal? absoluteAmount, decimal? percentageValue, string reason, CancellationToken ct = default)
        => client.SendApiAsync<AdminInvoiceDto>(
            new HttpRequestMessage(HttpMethod.Post, $"/api/v1/invoices/{invoiceId}/adjustments")
            {
                Content = JsonContent.Create(new
                {
                    Type = 0, // InvoiceAdjustmentRequestType.Discount
                    AbsoluteAmount = absoluteAmount,
                    PercentageValue = percentageValue,
                    Reason = reason,
                })
            }, ct);

    public Task<ApiResult<AdminInvoiceDto>> IssueCreditNoteAsync(
        Guid invoiceId, decimal amountNet, string reason, CancellationToken ct = default)
        => client.SendApiAsync<AdminInvoiceDto>(
            new HttpRequestMessage(HttpMethod.Post, $"/api/v1/invoices/{invoiceId}/adjustments")
            {
                Content = JsonContent.Create(new
                {
                    Type = 1, // InvoiceAdjustmentRequestType.Credit
                    AbsoluteAmount = amountNet,
                    PercentageValue = (decimal?)null,
                    Reason = reason,
                })
            }, ct);

    public string GetPdfUrl(Guid orderId) => $"/api/v1/invoices/by-order/{orderId}/pdf";
}
