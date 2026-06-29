using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Invoicing.Domain.Errors;

public static class InvoicingErrors
{
    public static readonly Error InvoiceNotFound = new(
        "Invoice.NotFound", "Invoice not found.", ErrorType.NotFound);

    public static readonly Error BlobUploadFailed = new(
        "Invoice.BlobUploadFailed", "PDF upload to blob storage failed.", ErrorType.Failure);
}
