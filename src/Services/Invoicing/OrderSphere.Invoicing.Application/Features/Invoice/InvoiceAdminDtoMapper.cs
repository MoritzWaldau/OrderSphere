using InvoiceEntity = OrderSphere.Invoicing.Domain.Entities.Invoice;

namespace OrderSphere.Invoicing.Application.Features.Invoice;

internal static class InvoiceAdminDtoMapper
{
    public static InvoiceAdminDto ToAdminDto(this InvoiceEntity invoice)
    {
        return new InvoiceAdminDto(
            invoice.Id.Value,
            invoice.InvoiceNumber,
            invoice.OrderId,
            invoice.CustomerName,
            invoice.CustomerEmail,
            invoice.Total,
            invoice.NetAmount,
            invoice.TaxRate,
            invoice.TaxAmount,
            invoice.AdjustedNet,
            invoice.AdjustedTax,
            invoice.AdjustedTotal,
            invoice.IssuedAt,
            invoice.Status.ToString(),
            [.. invoice.Adjustments
                .OrderByDescending(a => a.AppliedAt)
                .Select(a => new InvoiceAdjustmentDto(
                    a.Id.Value, a.Type.ToString(), a.AmountNet, a.Reason, a.AppliedBy, a.AppliedAt))]);
    }
}
