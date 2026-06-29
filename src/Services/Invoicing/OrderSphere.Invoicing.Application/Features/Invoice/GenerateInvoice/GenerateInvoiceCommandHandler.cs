using InvoiceEntity = OrderSphere.Invoicing.Domain.Entities.Invoice;

namespace OrderSphere.Invoicing.Application.Features.Invoice.GenerateInvoice;

public sealed record GenerateInvoiceCommand(
    Guid OrderId,
    string CustomerEmail,
    string CustomerName,
    decimal Total,
    IReadOnlyList<InvoiceItemDto> Items) : ICommand<Result<InvoiceCreatedDto>>;

public sealed class GenerateInvoiceCommandValidator : AbstractValidator<GenerateInvoiceCommand>
{
    public GenerateInvoiceCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CustomerEmail).NotEmpty();
        RuleFor(x => x.CustomerName).NotEmpty();
        RuleFor(x => x.Total).GreaterThan(0);
    }
}

public sealed class GenerateInvoiceCommandHandler(
    IInvoicingDbContext context,
    IInvoiceNumberGenerator numberGenerator,
    IInvoicePdfService pdfService,
    IBlobStorageService blobStorage) : ICommandHandler<GenerateInvoiceCommand, Result<InvoiceCreatedDto>>
{
    public async Task<Result<InvoiceCreatedDto>> Handle(GenerateInvoiceCommand request, CancellationToken ct)
    {
        var existing = await context.Invoices
            .FirstOrDefaultAsync(i => i.OrderId == request.OrderId, ct);

        if (existing is not null)
        {
            // Idempotent re-delivery: never draw a new number for an order that already has an invoice.
            var sasUrl = existing.BlobPath.Length > 0
                ? await blobStorage.GetSasUrlAsync(existing.BlobPath, ct)
                : string.Empty;
            return Result<InvoiceCreatedDto>.Success(new InvoiceCreatedDto(existing.InvoiceNumber, sasUrl));
        }

        var lineItems = request.Items
            .Select(i => new InvoiceLineItem { ProductName = i.ProductName, Quantity = i.Quantity, UnitPrice = i.UnitPrice })
            .ToList();

        var issuedAt = DateTime.UtcNow;
        var blobPath = string.Empty;

        // The number draw and the invoice insert share one transaction so the counter row stays locked
        // until commit — concurrent generations serialise and a rollback reverts the increment, keeping
        // numbering gapless. PDF render / blob upload run inside the transaction too; the row lock is
        // brief given the low consumer concurrency (InvoiceProcessor MaxConcurrentCalls = 2).
        await context.BeginTransactionAsync(ct);
        InvoiceEntity invoice;
        try
        {
            var invoiceNumber = await numberGenerator.NextAsync(issuedAt, ct);

            invoice = InvoiceEntity.Create(
                request.OrderId, request.CustomerEmail, request.CustomerName, request.Total,
                lineItems, invoiceNumber, issuedAt);

            var pdfBytes = await pdfService.GenerateAsync(invoice, ct);

            if (blobStorage.IsEnabled)
            {
                var blobName = $"invoices/{invoice.OrderId}/{invoice.InvoiceNumber}.pdf";
                using var stream = new MemoryStream(pdfBytes);
                blobPath = await blobStorage.UploadAsync(blobName, stream, "application/pdf", ct);
                invoice.SetBlobPath(blobPath);
            }

            context.Invoices.Add(invoice);
            await context.CommitAsync(ct);
        }
        catch
        {
            await context.RollbackAsync(ct);
            throw;
        }

        var downloadUrl = blobPath.Length > 0
            ? await blobStorage.GetSasUrlAsync(blobPath, ct)
            : string.Empty;

        return Result<InvoiceCreatedDto>.Success(new InvoiceCreatedDto(invoice.InvoiceNumber, downloadUrl));
    }
}
