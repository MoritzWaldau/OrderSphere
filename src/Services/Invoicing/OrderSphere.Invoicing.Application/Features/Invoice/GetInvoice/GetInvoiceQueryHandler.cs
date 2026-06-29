namespace OrderSphere.Invoicing.Application.Features.Invoice.GetInvoice;

public sealed record GetInvoiceByOrderIdQuery(Guid OrderId) : IQuery<Result<InvoiceDto>>;

public sealed class GetInvoiceByOrderIdQueryHandler(IInvoicingDbContext context)
    : IQueryHandler<GetInvoiceByOrderIdQuery, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(GetInvoiceByOrderIdQuery request, CancellationToken ct)
    {
        var invoice = await context.Invoices
            .FirstOrDefaultAsync(i => i.OrderId == request.OrderId, ct);

        if (invoice is null)
            return Result<InvoiceDto>.Failure(InvoicingErrors.InvoiceNotFound);

        return Result<InvoiceDto>.Success(new InvoiceDto(
            invoice.Id.Value,
            invoice.InvoiceNumber,
            invoice.OrderId,
            invoice.CustomerEmail,
            invoice.CustomerName,
            invoice.Total,
            invoice.IssuedAt));
    }
}
