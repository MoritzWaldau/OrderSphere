namespace OrderSphere.Invoicing.Infrastructure.Persistence;

/// <summary>
/// Single-row persistence counter backing the gapless invoice number sequence. Not a domain entity —
/// it is an infrastructure mechanism, so it does not inherit <c>AuditableEntity</c> and is not exposed
/// on <c>IInvoicingDbContext</c>. There is exactly one row (<see cref="Id"/> = 1).
/// </summary>
public sealed class InvoiceNumberCounter
{
    public int Id { get; set; }

    /// <summary>Last invoice number issued; the next allocation returns <c>Value + 1</c>.</summary>
    public long Value { get; set; }
}
