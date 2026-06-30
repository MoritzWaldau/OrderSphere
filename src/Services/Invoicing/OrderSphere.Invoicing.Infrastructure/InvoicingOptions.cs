namespace OrderSphere.Invoicing.Infrastructure;

public sealed class InvoicingOptions
{
    public const string SectionName = "Invoicing";

    /// <summary>VAT rate applied when an invoice is generated (one tax class per invoice).</summary>
    public decimal DefaultVatRate { get; set; } = 0.19m;
}
