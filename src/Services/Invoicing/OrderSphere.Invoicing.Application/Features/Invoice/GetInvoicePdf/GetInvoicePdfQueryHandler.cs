namespace OrderSphere.Invoicing.Application.Features.Invoice.GetInvoicePdf;

public sealed record GetInvoicePdfQuery(Guid OrderId) : IQuery<Result<InvoicePdfDto>>;

public sealed class GetInvoicePdfQueryHandler(IInvoicingDbContext context, IInvoicePdfService pdfService)
    : IQueryHandler<GetInvoicePdfQuery, Result<InvoicePdfDto>>
{
    public async Task<Result<InvoicePdfDto>> Handle(GetInvoicePdfQuery request, CancellationToken ct)
    {
        var invoice = await context.Invoices
            .FirstOrDefaultAsync(i => i.OrderId == request.OrderId, ct);

        if (invoice is null)
            return Result<InvoicePdfDto>.Failure(InvoicingErrors.InvoiceNotFound);

        // Regenerated from persisted invoice metadata so the download works regardless of
        // whether blob storage is configured (the blob/SAS path serves the email attachment).
        var bytes = await pdfService.GenerateAsync(invoice, ct);
        return Result<InvoicePdfDto>.Success(new InvoicePdfDto(bytes, $"{invoice.InvoiceNumber}.pdf"));
    }
}
