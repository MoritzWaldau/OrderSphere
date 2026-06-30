using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Invoicing.Application.Features.Invoice.ApplyDiscount;

public sealed record ApplyDiscountCommand(
    Guid InvoiceId,
    decimal? AbsoluteAmount,
    decimal? PercentageValue,
    string Reason,
    string AppliedBy) : ICommand<Result<InvoiceAdminDto>>;

public sealed class ApplyDiscountCommandValidator : AbstractValidator<ApplyDiscountCommand>
{
    public ApplyDiscountCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty();
        RuleFor(x => x.AppliedBy).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.AbsoluteAmount.HasValue ^ x.PercentageValue.HasValue)
            .WithMessage("Specify either an absolute amount or a percentage, not both.");
        RuleFor(x => x.AbsoluteAmount).GreaterThan(0).When(x => x.AbsoluteAmount.HasValue);
        RuleFor(x => x.PercentageValue).InclusiveBetween(0.01m, 100m).When(x => x.PercentageValue.HasValue);
    }
}

public sealed class ApplyDiscountCommandHandler(IInvoicingDbContext context)
    : ICommandHandler<ApplyDiscountCommand, Result<InvoiceAdminDto>>
{
    public async Task<Result<InvoiceAdminDto>> Handle(ApplyDiscountCommand request, CancellationToken ct)
    {
        var invoiceId = InvoiceId.From(request.InvoiceId);

        var invoice = await context.Invoices
            .Include(i => i.Adjustments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

        if (invoice is null)
            return Result<InvoiceAdminDto>.Failure(InvoicingErrors.InvoiceNotFound);

        // Percentage discounts resolve against the currently effective net (not the original),
        // so successive discounts compound on what the customer still owes.
        var amountNet = request.AbsoluteAmount
            ?? Math.Round(invoice.AdjustedNet * (request.PercentageValue!.Value / 100m), 2, MidpointRounding.AwayFromZero);

        var result = invoice.ApplyDiscount(amountNet, request.Reason, request.AppliedBy, DateTime.UtcNow);
        if (result.IsFailure)
            return Result<InvoiceAdminDto>.Failure(result.Error);

        await context.SaveChangesAsync(ct);
        return Result<InvoiceAdminDto>.Success(invoice.ToAdminDto());
    }
}
