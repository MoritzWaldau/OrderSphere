using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Invoicing.Domain.Enums;
using OrderSphere.Invoicing.Domain.Errors;

namespace OrderSphere.Invoicing.Domain.Entities;

public sealed class Invoice : AuditableEntity<InvoiceId>
{
    public string InvoiceNumber { get; private set; } = default!;
    public Guid OrderId { get; private set; }
    public string CustomerEmail { get; private set; } = default!;
    public string CustomerName { get; private set; } = default!;
    public decimal Total { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal TaxAmount { get; private set; }
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Issued;
    public string BlobPath { get; private set; } = string.Empty;
    public DateTime IssuedAt { get; private set; }
    public List<InvoiceLineItem> Items { get; private set; } = [];
    public List<InvoiceAdjustment> Adjustments { get; private set; } = [];

    // Original Total/NetAmount/TaxAmount stay immutable for revision-safety; these compute the
    // currently effective amounts from the original net minus all applied adjustments.
    public decimal AdjustedNet => NetAmount - Adjustments.Sum(a => a.AmountNet);
    public decimal AdjustedTax => Math.Round(AdjustedNet * TaxRate, 2, MidpointRounding.AwayFromZero);
    public decimal AdjustedTotal => AdjustedNet + AdjustedTax;

    private Invoice() { }

    // The sequential invoice number and issue timestamp are allocated by the application layer
    // (see IInvoiceNumberGenerator) and passed in, so the domain constructor stays pure and
    // deterministic — no clock or counter access here. The order's Total is treated as gross;
    // Net/Tax are derived from it using the configured tax rate (one tax class per invoice).
    public static Invoice Create(
        Guid orderId,
        string customerEmail,
        string customerName,
        decimal total,
        IReadOnlyList<InvoiceLineItem> items,
        string invoiceNumber,
        DateTime issuedAt,
        decimal taxRate)
    {
        var net = Math.Round(total / (1 + taxRate), 2, MidpointRounding.AwayFromZero);
        var tax = total - net;

        return new Invoice
        {
            Id = InvoiceId.New(),
            InvoiceNumber = invoiceNumber,
            OrderId = orderId,
            CustomerEmail = customerEmail,
            CustomerName = customerName,
            Total = total,
            NetAmount = net,
            TaxRate = taxRate,
            TaxAmount = tax,
            Status = InvoiceStatus.Issued,
            IssuedAt = issuedAt,
            Items = [.. items],
        };
    }

    public void SetBlobPath(string blobPath) => BlobPath = blobPath;

    public Result ApplyDiscount(decimal amountNet, string reason, string appliedBy, DateTime appliedAt)
    {
        var validation = ValidateAdjustmentAmount(amountNet);
        if (validation.IsFailure)
            return validation;

        Adjustments.Add(InvoiceAdjustment.Create(
            Id, InvoiceAdjustmentType.Discount, amountNet, reason, appliedBy, appliedAt));
        Status = InvoiceStatus.Adjusted;
        return Result.Success();
    }

    public Result IssueCreditNote(decimal amountNet, string reason, string appliedBy, DateTime appliedAt)
    {
        var validation = ValidateAdjustmentAmount(amountNet);
        if (validation.IsFailure)
            return validation;

        Adjustments.Add(InvoiceAdjustment.Create(
            Id, InvoiceAdjustmentType.Credit, amountNet, reason, appliedBy, appliedAt));
        Status = InvoiceStatus.CreditIssued;
        return Result.Success();
    }

    private Result ValidateAdjustmentAmount(decimal amountNet)
    {
        if (amountNet <= 0)
            return Result.Failure(InvoicingErrors.AdjustmentAmountInvalid);

        if (AdjustedNet - amountNet < 0)
            return Result.Failure(InvoicingErrors.AdjustmentExceedsNet);

        return Result.Success();
    }
}
