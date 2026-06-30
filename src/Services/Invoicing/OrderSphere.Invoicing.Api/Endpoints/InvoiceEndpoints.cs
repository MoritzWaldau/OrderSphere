using MediatR;
using OrderSphere.BuildingBlocks.Security;
using OrderSphere.Invoicing.Application.Features.Invoice.ApplyDiscount;
using OrderSphere.Invoicing.Application.Features.Invoice.GetInvoice;
using OrderSphere.Invoicing.Application.Features.Invoice.GetInvoiceById;
using OrderSphere.Invoicing.Application.Features.Invoice.GetInvoiceByNumber;
using OrderSphere.Invoicing.Application.Features.Invoice.GetInvoiceDownloadUrl;
using OrderSphere.Invoicing.Application.Features.Invoice.GetInvoicePdf;
using OrderSphere.Invoicing.Application.Features.Invoice.IssueCreditNote;
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
            .WithSummary("Streams the invoice PDF. Inline by default; pass ?download=true for an attachment.");

        group.MapGet("by-number/{invoiceNumber}", GetInvoiceByNumber)
            .RequireAuthorization("AdminPolicy")
            .WithName("GetInvoiceByNumber")
            .WithSummary("Admin support lookup of a single invoice by its invoice number.");

        group.MapGet("by-id/{invoiceId:guid}", GetInvoiceById)
            .RequireAuthorization("AdminPolicy")
            .WithName("GetInvoiceById")
            .WithSummary("Admin detail view of a single invoice, including amounts and adjustment history.");

        group.MapPost("{invoiceId:guid}/adjustments", ApplyAdjustment)
            .RequireAuthorization("AdminPolicy")
            .WithName("ApplyInvoiceAdjustment")
            .WithSummary("Applies a discount or issues a credit note against an invoice.");
    }

    private static async Task<IResult> GetInvoiceByNumber(
        string invoiceNumber, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetInvoiceByNumberQuery(invoiceNumber), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetInvoiceById(
        Guid invoiceId, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetInvoiceByIdQuery(invoiceId), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ApplyAdjustment(
        Guid invoiceId, ApplyInvoiceAdjustmentRequest request,
        ICurrentUser currentUser, ISender sender, CancellationToken ct)
    {
        var appliedBy = currentUser.Email ?? string.Empty;

        return request.Type switch
        {
            InvoiceAdjustmentRequestType.Discount => (await sender.Send(
                new ApplyDiscountCommand(invoiceId, request.AbsoluteAmount, request.PercentageValue, request.Reason, appliedBy), ct))
                .ToHttpResult(),
            InvoiceAdjustmentRequestType.Credit => (await sender.Send(
                new IssueCreditNoteCommand(invoiceId, request.AbsoluteAmount ?? 0, request.Reason, appliedBy), ct))
                .ToHttpResult(),
            _ => Results.BadRequest($"Unknown adjustment type '{request.Type}'."),
        };
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
        Guid orderId, ICurrentUser currentUser, ISender sender, HttpContext http,
        bool download, CancellationToken ct)
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

        // Inline lets the browser render the PDF in a new tab; download=true forces a file save.
        // Either way the filename is honoured when the user chooses to save.
        var disposition = download ? "attachment" : "inline";
        http.Response.Headers.ContentDisposition = $"{disposition}; filename=\"{pdfResult.Value.FileName}\"";
        return Results.File(pdfResult.Value.Content, "application/pdf");
    }
}

public enum InvoiceAdjustmentRequestType
{
    Discount,
    Credit
}

public sealed record ApplyInvoiceAdjustmentRequest(
    InvoiceAdjustmentRequestType Type,
    decimal? AbsoluteAmount,
    decimal? PercentageValue,
    string Reason);
