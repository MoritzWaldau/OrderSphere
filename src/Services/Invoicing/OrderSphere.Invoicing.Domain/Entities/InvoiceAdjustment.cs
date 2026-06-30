using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Invoicing.Domain.Enums;

namespace OrderSphere.Invoicing.Domain.Entities;

public sealed class InvoiceAdjustment : AuditableEntity<InvoiceAdjustmentId>
{
    public InvoiceId InvoiceId { get; private set; }
    public InvoiceAdjustmentType Type { get; private set; }
    public decimal AmountNet { get; private set; }
    public string Reason { get; private set; } = default!;
    public string AppliedBy { get; private set; } = default!;
    public DateTime AppliedAt { get; private set; }

    private InvoiceAdjustment() { }

    // Created only through Invoice.ApplyDiscount/IssueCreditNote so the parent aggregate
    // can enforce the "adjusted net never below zero" invariant before an instance exists.
    internal static InvoiceAdjustment Create(
        InvoiceId invoiceId,
        InvoiceAdjustmentType type,
        decimal amountNet,
        string reason,
        string appliedBy,
        DateTime appliedAt)
    {
        return new InvoiceAdjustment
        {
            Id = InvoiceAdjustmentId.New(),
            InvoiceId = invoiceId,
            Type = type,
            AmountNet = amountNet,
            Reason = reason,
            AppliedBy = appliedBy,
            AppliedAt = appliedAt,
        };
    }
}
