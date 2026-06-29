using MediatR;
using OrderSphere.BuildingBlocks.Security;
using OrderSphere.Invoicing.Application.Features.Invoice.GetInvoice;
using OrderSphere.Invoicing.Application.Features.Invoice.GetInvoiceDownloadUrl;
using OrderSphere.Invoicing.Application.Features.Invoice.GetInvoicePdf;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Invoicing.Api.Endpoints;

public static class InvoiceEndpoints
{
    public static void MapInvoiceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("api/v1/invoices").RequireAuthorization();

        group.MapGet("by-order/{orderId:guid}", GetInvoiceByOrder)
            .WithName("GetInvoiceByOrder")
            .WithSummary("Returns invoice metadata for the given order.");

        group.MapGet("by-order/{orderId:guid}/download", GetDownloadUrl)
            .WithName("GetInvoiceDownloadUrl")
            .WithSummary("Returns a short-lived SAS download URL for the invoice PDF.");

        group.MapGet("by-order/{orderId:guid}/pdf", GetPdf)
            .WithName("GetInvoicePdf")
            .WithSummary("Streams the invoice PDF inline for viewing/downloading.");
    }

    private static async Task<IResult> GetInvoiceByOrder(
        Guid orderId, ICurrentUser currentUser, ISender sender, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var result = await sender.Send(new GetInvoiceByOrderIdQuery(orderId), ct);

        if (result.IsFailure)
            return result.ToHttpResult();

        if (!currentUser.IsInRole("admin") && result.Value.CustomerEmail != currentUser.Email)
            return Results.Forbid();

        return result.ToHttpResult();
    }

    private static async Task<IResult> GetDownloadUrl(
        Guid orderId, ICurrentUser currentUser, ISender sender, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var invoiceResult = await sender.Send(new GetInvoiceByOrderIdQuery(orderId), ct);
        if (invoiceResult.IsFailure)
            return invoiceResult.ToHttpResult();

        if (!currentUser.IsInRole("admin") && invoiceResult.Value.CustomerEmail != currentUser.Email)
            return Results.Forbid();

        var urlResult = await sender.Send(new GetInvoiceDownloadUrlQuery(orderId), ct);
        return urlResult.ToHttpResult();
    }

    private static async Task<IResult> GetPdf(
        Guid orderId, ICurrentUser currentUser, ISender sender, HttpContext http, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var invoiceResult = await sender.Send(new GetInvoiceByOrderIdQuery(orderId), ct);
        if (invoiceResult.IsFailure)
            return invoiceResult.ToHttpResult();

        if (!currentUser.IsInRole("admin") && invoiceResult.Value.CustomerEmail != currentUser.Email)
            return Results.Forbid();

        var pdfResult = await sender.Send(new GetInvoicePdfQuery(orderId), ct);
        if (pdfResult.IsFailure)
            return pdfResult.ToHttpResult();

        // Inline disposition so the browser can render the PDF in a new tab; the filename is
        // still honoured when the user chooses to save.
        http.Response.Headers.ContentDisposition = $"inline; filename=\"{pdfResult.Value.FileName}\"";
        return Results.File(pdfResult.Value.Content, "application/pdf");
    }
}
