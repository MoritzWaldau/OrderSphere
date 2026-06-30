using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Invoicing.Domain.Errors;

public static class InvoicingErrors
{
    public static readonly Error InvoiceNotFound = new(
        "Invoice.NotFound", "Invoice not found.", ErrorType.NotFound);

    public static readonly Error BlobUploadFailed = new(
        "Invoice.BlobUploadFailed", "PDF upload to blob storage failed.", ErrorType.Failure);

    public static readonly Error AdjustmentAmountInvalid = new(
        "Invoice.AdjustmentAmountInvalid", "The adjustment amount must be greater than zero.", ErrorType.Validation);

    public static readonly Error AdjustmentExceedsNet = new(
        "Invoice.AdjustmentExceedsNet", "The adjustment would reduce the invoice's net amount below zero.", ErrorType.Validation);
}
