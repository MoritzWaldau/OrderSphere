using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Invoicing.Application.Features.Invoice.IssueCreditNote;

public sealed record IssueCreditNoteCommand(
    Guid InvoiceId,
    decimal AmountNet,
    string Reason,
    string AppliedBy) : ICommand<Result<InvoiceAdminDto>>;

public sealed class IssueCreditNoteCommandValidator : AbstractValidator<IssueCreditNoteCommand>
{
    public IssueCreditNoteCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.AmountNet).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty();
        RuleFor(x => x.AppliedBy).NotEmpty();
    }
}

public sealed class IssueCreditNoteCommandHandler(IInvoicingDbContext context)
    : ICommandHandler<IssueCreditNoteCommand, Result<InvoiceAdminDto>>
{
    public async Task<Result<InvoiceAdminDto>> Handle(IssueCreditNoteCommand request, CancellationToken ct)
    {
        var invoiceId = InvoiceId.From(request.InvoiceId);

        var invoice = await context.Invoices
            .Include(i => i.Adjustments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

        if (invoice is null)
            return Result<InvoiceAdminDto>.Failure(InvoicingErrors.InvoiceNotFound);

        var result = invoice.IssueCreditNote(request.AmountNet, request.Reason, request.AppliedBy, DateTime.UtcNow);
        if (result.IsFailure)
            return Result<InvoiceAdminDto>.Failure(result.Error);

        await context.SaveChangesAsync(ct);
        return Result<InvoiceAdminDto>.Success(invoice.ToAdminDto());
    }
}
