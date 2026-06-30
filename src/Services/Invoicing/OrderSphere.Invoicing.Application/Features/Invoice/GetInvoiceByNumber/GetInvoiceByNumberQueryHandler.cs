namespace OrderSphere.Invoicing.Application.Features.Invoice.GetInvoiceByNumber;

public sealed record GetInvoiceByNumberQuery(string InvoiceNumber) : IQuery<Result<InvoiceAdminDto>>;

public sealed class GetInvoiceByNumberQueryHandler(IInvoicingDbContext context)
    : IQueryHandler<GetInvoiceByNumberQuery, Result<InvoiceAdminDto>>
{
    public async Task<Result<InvoiceAdminDto>> Handle(GetInvoiceByNumberQuery request, CancellationToken ct)
    {
        var invoiceNumber = request.InvoiceNumber.Trim();

        var invoice = await context.Invoices
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, ct);

        if (invoice is null)
            return Result<InvoiceAdminDto>.Failure(InvoicingErrors.InvoiceNotFound);

        return Result<InvoiceAdminDto>.Success(new InvoiceAdminDto(
            invoice.Id.Value,
            invoice.InvoiceNumber,
            invoice.OrderId,
            invoice.CustomerName,
            invoice.CustomerEmail,
            invoice.Total,
            invoice.IssuedAt,
            "Issued"));
    }
}
