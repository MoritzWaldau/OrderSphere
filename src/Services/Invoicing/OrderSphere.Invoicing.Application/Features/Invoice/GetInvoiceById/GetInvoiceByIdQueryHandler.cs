using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Invoicing.Application.Features.Invoice.GetInvoiceById;

public sealed record GetInvoiceByIdQuery(Guid InvoiceId) : IQuery<Result<InvoiceAdminDto>>;

public sealed class GetInvoiceByIdQueryHandler(IInvoicingDbContext context)
    : IQueryHandler<GetInvoiceByIdQuery, Result<InvoiceAdminDto>>
{
    public async Task<Result<InvoiceAdminDto>> Handle(GetInvoiceByIdQuery request, CancellationToken ct)
    {
        var invoiceId = InvoiceId.From(request.InvoiceId);

        var invoice = await context.Invoices
            .Include(i => i.Adjustments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

        if (invoice is null)
            return Result<InvoiceAdminDto>.Failure(InvoicingErrors.InvoiceNotFound);

        return Result<InvoiceAdminDto>.Success(invoice.ToAdminDto());
    }
}
