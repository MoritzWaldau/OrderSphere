namespace OrderSphere.Invoicing.Application.Features.Invoice.GetInvoiceDownloadUrl;

public sealed record GetInvoiceDownloadUrlQuery(Guid OrderId) : IQuery<Result<string>>;

public sealed class GetInvoiceDownloadUrlQueryHandler(IInvoicingDbContext context, IBlobStorageService blobStorage)
    : IQueryHandler<GetInvoiceDownloadUrlQuery, Result<string>>
{
    public async Task<Result<string>> Handle(GetInvoiceDownloadUrlQuery request, CancellationToken ct)
    {
        var invoice = await context.Invoices
            .FirstOrDefaultAsync(i => i.OrderId == request.OrderId, ct);

        if (invoice is null)
            return Result<string>.Failure(InvoicingErrors.InvoiceNotFound);

        if (!blobStorage.IsEnabled || invoice.BlobPath.Length == 0)
            return Result<string>.Success(string.Empty);

        var url = await blobStorage.GetSasUrlAsync(invoice.BlobPath, ct);
        return Result<string>.Success(url);
    }
}
