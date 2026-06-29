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
    IInvoicePdfService pdfService,
    IBlobStorageService blobStorage) : ICommandHandler<GenerateInvoiceCommand, Result<InvoiceCreatedDto>>
{
    public async Task<Result<InvoiceCreatedDto>> Handle(GenerateInvoiceCommand request, CancellationToken ct)
    {
        var existing = await context.Invoices
            .FirstOrDefaultAsync(i => i.OrderId == request.OrderId, ct);

        if (existing is not null)
        {
            var sasUrl = existing.BlobPath.Length > 0
                ? await blobStorage.GetSasUrlAsync(existing.BlobPath, ct)
                : string.Empty;
            return Result<InvoiceCreatedDto>.Success(new InvoiceCreatedDto(existing.InvoiceNumber, sasUrl));
        }

        var lineItems = request.Items
            .Select(i => new InvoiceLineItem { ProductName = i.ProductName, Quantity = i.Quantity, UnitPrice = i.UnitPrice })
            .ToList();

        var invoice = InvoiceEntity.Create(
            request.OrderId, request.CustomerEmail, request.CustomerName, request.Total, lineItems);

        var pdfBytes = await pdfService.GenerateAsync(invoice, ct);

        var blobPath = string.Empty;
        if (blobStorage.IsEnabled)
        {
            var blobName = $"invoices/{invoice.OrderId}/{invoice.InvoiceNumber}.pdf";
            using var stream = new MemoryStream(pdfBytes);
            blobPath = await blobStorage.UploadAsync(blobName, stream, "application/pdf", ct);
            invoice.SetBlobPath(blobPath);
        }

        context.Invoices.Add(invoice);
        await context.SaveChangesAsync(ct);

        var downloadUrl = blobPath.Length > 0
            ? await blobStorage.GetSasUrlAsync(blobPath, ct)
            : string.Empty;

        return Result<InvoiceCreatedDto>.Success(new InvoiceCreatedDto(invoice.InvoiceNumber, downloadUrl));
    }
}
